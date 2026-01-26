(function ($) {
    "use strict";

    // Spinner
    var spinner = function () {
        setTimeout(function () {
            if ($('#spinner').length > 0) {
                $('#spinner').removeClass('show');
            }
        }, 1);
    };
    spinner(0);


    // Fixed Navbar
    $(window).scroll(function () {
        if ($(window).width() < 992) {
            if ($(this).scrollTop() > 55) {
                $('.fixed-top').addClass('shadow');
            } else {
                $('.fixed-top').removeClass('shadow');
            }
        } else {
            if ($(this).scrollTop() > 55) {
                $('.fixed-top').addClass('shadow').css('top', -55);
            } else {
                $('.fixed-top').removeClass('shadow').css('top', 0);
            }
        } 
    });
    
    
   // Back to top button
   $(window).scroll(function () {
    if ($(this).scrollTop() > 300) {
        $('.back-to-top').fadeIn('slow');
    } else {
        $('.back-to-top').fadeOut('slow');
    }
    });
    $('.back-to-top').click(function () {
        $('html, body').animate({scrollTop: 0}, 1500, 'easeInOutExpo');
        return false;
    });


    // Testimonial carousel
    $(".testimonial-carousel").owlCarousel({
        autoplay: true,
        smartSpeed: 2000,
        center: false,
        dots: true,
        loop: true,
        margin: 25,
        nav : true,
        navText : [
            '<i class="bi bi-arrow-left"></i>',
            '<i class="bi bi-arrow-right"></i>'
        ],
        responsiveClass: true,
        responsive: {
            0:{
                items:1
            },
            576:{
                items:1
            },
            768:{
                items:1
            },
            992:{
                items:2
            },
            1200:{
                items:2
            }
        }
    });


    // vegetable carousel
    $(".vegetable-carousel").owlCarousel({
        autoplay: true,
        smartSpeed: 1500,
        center: false,
        dots: true,
        loop: true,
        margin: 25,
        nav : true,
        navText : [
            '<i class="bi bi-arrow-left"></i>',
            '<i class="bi bi-arrow-right"></i>'
        ],
        responsiveClass: true,
        responsive: {
            0:{
                items:1
            },
            576:{
                items:1
            },
            768:{
                items:2
            },
            992:{
                items:3
            },
            1200:{
                items:4
            }
        }
    });


    // Modal Video
    $(document).ready(function () {
        var $videoSrc;
        $('.btn-play').click(function () {
            $videoSrc = $(this).data("src");
        });
        console.log($videoSrc);

        $('#videoModal').on('shown.bs.modal', function (e) {
            $("#video").attr('src', $videoSrc + "?autoplay=1&amp;modestbranding=1&amp;showinfo=0");
        })

        $('#videoModal').on('hide.bs.modal', function (e) {
            $("#video").attr('src', $videoSrc);
        })
    });



    // Product Quantity (REMOVE - replaced with specific handlers below)
    // This code was causing double increment issues

    // Add to Cart functionality
    $(document).on('click', '.btn-add-to-cart', function (e) {
        e.preventDefault();
        var merchandiseId = $(this).data('id');
        
        $.ajax({
            url: '/Cart/AddToCart',
            type: 'POST',
            data: { id: merchandiseId, quantity: 1 },
            success: function (response) {
                if (response.success) {
                    // Update cart count
                    $('#cart-count').text(response.cartCount).show();
                    
                    // Show success message
                    alert('Product added to cart successfully!');
                }
            },
            error: function () {
                alert('Error adding product to cart. Please try again.');
            }
        });
    });

    // Add to Cart from Detail page with quantity
    $(document).on('click', '.btn-add-to-cart-detail', function (e) {
        e.preventDefault();
        var merchandiseId = $(this).data('id');
        var quantity = parseInt($('#quantity-input').val()) || 1;
        
        $.ajax({
            url: '/Cart/AddToCart',
            type: 'POST',
            data: { id: merchandiseId, quantity: quantity },
            success: function (response) {
                if (response.success) {
                    // Update cart count
                    $('#cart-count').text(response.cartCount).show();
                    
                    // Show success message
                    alert('Product added to cart successfully!');
                }
            },
            error: function () {
                alert('Error adding product to cart. Please try again.');
            }
        });
    });

    // Quantity buttons for detail page
    $('#btn-plus').on('click', function (e) {
        e.preventDefault();
        var currentVal = parseInt($('#quantity-input').val()) || 1;
        $('#quantity-input').val(currentVal + 1);
    });

    $('#btn-minus').on('click', function (e) {
        e.preventDefault();
        var currentVal = parseInt($('#quantity-input').val()) || 1;
        if (currentVal > 1) {
            $('#quantity-input').val(currentVal - 1);
        }
    });

    // Cart page - Update quantity (Plus button)
    $(document).on('click', '.btn-plus-cart', function (e) {
        e.preventDefault();
        var merchandiseId = $(this).data('id');
        var $row = $('tr[data-id="' + merchandiseId + '"]');
        var currentQuantity = parseInt($row.find('.item-quantity').val());
        var newQuantity = currentQuantity + 1;

        updateCartQuantity(merchandiseId, newQuantity, $row);
    });

    // Cart page - Update quantity (Minus button)
    $(document).on('click', '.btn-minus-cart', function (e) {
        e.preventDefault();
        var merchandiseId = $(this).data('id');
        var $row = $('tr[data-id="' + merchandiseId + '"]');
        var currentQuantity = parseInt($row.find('.item-quantity').val());
        
        if (currentQuantity > 1) {
            var newQuantity = currentQuantity - 1;
            updateCartQuantity(merchandiseId, newQuantity, $row);
        }
    });

    // Cart page - Remove item
    $(document).on('click', '.btn-remove-cart', function (e) {
        e.preventDefault();
        var merchandiseId = $(this).data('id');
        
        if (confirm('Are you sure you want to remove this item?')) {
            $.ajax({
                url: '/Cart/RemoveFromCart',
                type: 'POST',
                data: { id: merchandiseId },
                success: function (response) {
                    if (response.success) {
                        // Remove row from table
                        $('tr[data-id="' + merchandiseId + '"]').fadeOut(300, function() {
                            $(this).remove();
                            
                            // Update cart count
                            $('#cart-count').text(response.cartCount);
                            if (response.cartCount === 0) {
                                $('#cart-count').hide();
                            }

                            // Check if cart is empty
                            if (response.isEmpty) {
                                location.reload();
                            } else {
                                // Update totals
                                updateCartTotals(response.subtotal);
                            }
                        });
                    }
                },
                error: function () {
                    alert('Error removing item from cart. Please try again.');
                }
            });
        }
    });

    // Function to update cart quantity via AJAX
    function updateCartQuantity(merchandiseId, quantity, $row) {
        $.ajax({
            url: '/Cart/UpdateQuantity',
            type: 'POST',
            data: { id: merchandiseId, quantity: quantity },
            success: function (response) {
                if (response.success) {
                    if (response.removed) {
                        // Item was removed (quantity = 0)
                        $row.fadeOut(300, function() {
                            $(this).remove();
                            
                            // Update cart count
                            $('#cart-count').text(response.cartCount);
                            if (response.cartCount === 0) {
                                $('#cart-count').hide();
                            }

                            // Check if cart is empty
                            if (response.isEmpty) {
                                location.reload();
                            } else {
                                // Update totals
                                updateCartTotals(response.subtotal);
                            }
                        });
                    } else {
                        // Update quantity display
                        $row.find('.item-quantity').val(response.quantity);
                        
                        // Update item total
                        $row.find('.item-total').text('$' + response.itemTotal.toFixed(2));
                        
                        // Update cart totals
                        updateCartTotals(response.subtotal);
                        
                        // Update cart count in header
                        $('#cart-count').text(response.cartCount).show();
                    }
                }
            },
            error: function () {
                alert('Error updating cart. Please try again.');
            }
        });
    }

    // Function to update cart totals
    function updateCartTotals(subtotal) {
        $('.cart-subtotal').text('$' + subtotal.toFixed(2));
        $('.cart-total').text('$' + subtotal.toFixed(2));
    }

})(jQuery);

