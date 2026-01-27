// Token Auto Refresh Handler
(function() {
    'use strict';

    // Get cookie value
    function getCookie(name) {
        const value = `; ${document.cookie}`;
        const parts = value.split(`; ${name}=`);
        if (parts.length === 2) return parts.pop().split(';').shift();
        return null;
    }

    // Parse JWT token to get expiration
    function parseJwt(token) {
        try {
            const base64Url = token.split('.')[1];
            const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
            const jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
                return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
            }).join(''));
            return JSON.parse(jsonPayload);
        } catch (e) {
            return null;
        }
    }

    // Check if token needs refresh (within 30 seconds of expiry)
    function shouldRefreshToken(token) {
        const payload = parseJwt(token);
        if (!payload || !payload.exp) return false;
        
        const expirationTime = payload.exp * 1000; // Convert to milliseconds
        const currentTime = Date.now();
        const timeUntilExpiry = expirationTime - currentTime;
        
        // Refresh if less than 30 seconds until expiry
        return timeUntilExpiry < 30000 && timeUntilExpiry > 0;
    }

    // Check if token is expired
    function isTokenExpired(token) {
        const payload = parseJwt(token);
        if (!payload || !payload.exp) return true;
        
        const expirationTime = payload.exp * 1000;
        const currentTime = Date.now();
        
        return currentTime >= expirationTime;
    }

    // Refresh access token
    async function refreshAccessToken() {
        try {
            const response = await fetch('/Account/RefreshToken', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                credentials: 'same-origin'
            });

            const data = await response.json();
            
            if (data.success) {
                console.log('Token refreshed successfully');
                return true;
            } else {
                console.error('Token refresh failed:', data.message);
                // Redirect to login if refresh fails
                window.location.href = '/Account/Login';
                return false;
            }
        } catch (error) {
            console.error('Error refreshing token:', error);
            return false;
        }
    }

    // Check if remember me is enabled (has refresh token cookie)
    function hasRememberMe() {
        return getCookie('fruitables_rf') !== null;
    }

    // Monitor token and refresh if needed
    function startTokenMonitoring() {
        const accessToken = getCookie('fruitables_ac');
        
        if (!accessToken) {
            console.log('No access token found');
            return;
        }

        // Check if token is already expired
        if (isTokenExpired(accessToken)) {
            console.log('Token expired, clearing cookies');
            document.cookie = 'fruitables_ac=; Max-Age=0; path=/;';
            document.cookie = 'fruitables_rf=; Max-Age=0; path=/;';
            
            // Redirect to login if not already on login page
            if (!window.location.pathname.includes('/Account/Login')) {
                window.location.href = '/Account/Login';
            }
            return;
        }

        // Only auto-refresh if remember me is enabled
        if (!hasRememberMe()) {
            console.log('Remember me not enabled, token will expire after session');
            return;
        }

        setInterval(async () => {
            const currentToken = getCookie('fruitables_ac');
            
            if (!currentToken) {
                console.log('Token not found, stopping monitoring');
                return;
            }

            // Check if expired
            if (isTokenExpired(currentToken)) {
                console.log('Token expired, attempting refresh');
                const refreshed = await refreshAccessToken();
                
                if (!refreshed) {
                    // Redirect to login
                    window.location.href = '/Account/Login';
                }
                return;
            }

            // Check if needs refresh soon
            if (shouldRefreshToken(currentToken)) {
                console.log('Token nearing expiration, refreshing...');
                await refreshAccessToken();
            }
        }, 10000); // Check every 10 seconds
    }

    // Handle page unload (user leaving the page)
    window.addEventListener('beforeunload', function(e) {
        const accessToken = getCookie('fruitables_ac');
        
        // If no remember me and has token, it will be cleared when browser closes
        // This is handled by the session cookie automatically
        if (!hasRememberMe() && accessToken) {
            console.log('Session-only token will be cleared when browser closes');
        }
    });

    // Start monitoring when page loads
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', startTokenMonitoring);
    } else {
        startTokenMonitoring();
    }

})();
