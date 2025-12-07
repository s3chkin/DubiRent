// Property Details Page JavaScript

// Fullscreen Image Modal Variables
let currentFullscreenIndex = 0;
let allImages = [];
let fullscreenModal = null;
let fullscreenImage = null;

// Image Gallery Functionality
document.addEventListener('DOMContentLoaded', function() {
    const mainImage = document.getElementById('mainImage');
    const thumbnails = document.querySelectorAll('.thumbnail-image');
    
    // Initialize fullscreen modal
    fullscreenModal = document.getElementById('fullscreenModal');
    fullscreenImage = document.getElementById('fullscreenImage');
    
    // Collect all images
    allImages = [];
    thumbnails.forEach(thumbnail => {
        const imageUrl = thumbnail.getAttribute('data-full-image');
        if (imageUrl && !allImages.includes(imageUrl)) {
            allImages.push(imageUrl);
        }
    });
    // If no thumbnails, use main image
    if (allImages.length === 0 && mainImage) {
        allImages.push(mainImage.src);
    }

    // Add click event to main image for fullscreen
    if (mainImage) {
        mainImage.addEventListener('click', function() {
            const currentSrc = this.src;
            const index = allImages.findIndex(url => url === currentSrc);
            openFullscreen(index >= 0 ? index : 0);
        });
    }

    // Add click events to thumbnails (only change main image, don't open fullscreen)
    thumbnails.forEach((thumbnail, index) => {
        thumbnail.addEventListener('click', function(e) {
            const fullImageUrl = this.getAttribute('data-full-image');
            
            // Update main image
            if (mainImage) {
                mainImage.style.opacity = '0';
                setTimeout(() => {
                    mainImage.src = fullImageUrl;
                    mainImage.style.opacity = '1';
                }, 200);
            }

            // Update active class
            thumbnails.forEach(t => t.classList.remove('active'));
            this.classList.add('active');
        });
    });

    // Close modal on Escape key
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape' && fullscreenModal && fullscreenModal.classList.contains('active')) {
            closeFullscreen();
        } else if (e.key === 'ArrowLeft' && fullscreenModal && fullscreenModal.classList.contains('active')) {
            changeFullscreenImage(-1);
        } else if (e.key === 'ArrowRight' && fullscreenModal && fullscreenModal.classList.contains('active')) {
            changeFullscreenImage(1);
        }
    });

    // Close modal when clicking outside the image
    if (fullscreenModal) {
        fullscreenModal.addEventListener('click', function(e) {
            if (e.target === fullscreenModal || e.target.classList.contains('fullscreen-modal-content')) {
                closeFullscreen();
            }
        });
    }

    // Favourite button functionality
    const favouriteBtn = document.getElementById('favouriteBtn');
    if (favouriteBtn) {
        favouriteBtn.addEventListener('click', async function() {
            const propertyId = this.getAttribute('data-property-id');
            if (!propertyId) return;

            const icon = this.querySelector('i');

            try {
                const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
                if (!tokenInput) {
                    alert('Security token not found. Please refresh the page.');
                    return;
                }

                const token = tokenInput.value;
                const formData = new URLSearchParams();
                formData.append('propertyId', propertyId);
                formData.append('__RequestVerificationToken', token);

                // Get the ToggleFavourite URL - use data attribute or construct it
                const toggleFavouriteUrl = favouriteBtn.getAttribute('data-toggle-favourite-url') || '/Property/ToggleFavourite';

                const response = await fetch(toggleFavouriteUrl, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded',
                        'RequestVerificationToken': token
                    },
                    body: formData.toString()
                });

                const result = await response.json();

                if (result.success) {
                    // Update button appearance
                    if (result.isFavourite) {
                        icon.classList.remove('far');
                        icon.classList.add('fas');
                        this.style.color = '#fe5658';
                        this.style.background = 'rgba(254, 86, 88, 0.1)';
                    } else {
                        icon.classList.remove('fas');
                        icon.classList.add('far');
                        this.style.color = '#6b7280';
                        this.style.background = 'transparent';
                    }
                } else {
                    alert(result.message || 'An error occurred. Please try again.');
                }
            } catch (error) {
                console.error('Error toggling favourite:', error);
                alert('An error occurred. Please try again.');
            }
        });
    }
});

// Fullscreen Modal Functions
function openFullscreen(index) {
    if (!fullscreenModal || !fullscreenImage || allImages.length === 0) return;
    
    currentFullscreenIndex = Math.max(0, Math.min(index, allImages.length - 1));
    fullscreenImage.src = allImages[currentFullscreenIndex];
    fullscreenModal.classList.add('active');
    document.body.style.overflow = 'hidden';
    updateFullscreenNavigation();
    updateImageCounter();
}

function closeFullscreen() {
    if (!fullscreenModal) return;
    fullscreenModal.classList.remove('active');
    document.body.style.overflow = '';
}

function changeFullscreenImage(direction) {
    if (allImages.length === 0) return;
    
    currentFullscreenIndex += direction;
    
    if (currentFullscreenIndex < 0) {
        currentFullscreenIndex = allImages.length - 1;
    } else if (currentFullscreenIndex >= allImages.length) {
        currentFullscreenIndex = 0;
    }
    
    if (fullscreenImage) {
        fullscreenImage.src = allImages[currentFullscreenIndex];
    }
    updateFullscreenNavigation();
    updateImageCounter();
}

function updateFullscreenNavigation() {
    const prevBtn = document.getElementById('prevBtn');
    const nextBtn = document.getElementById('nextBtn');
    
    if (allImages.length <= 1) {
        if (prevBtn) prevBtn.style.display = 'none';
        if (nextBtn) nextBtn.style.display = 'none';
    } else {
        if (prevBtn) prevBtn.style.display = 'flex';
        if (nextBtn) nextBtn.style.display = 'flex';
    }
}

function updateImageCounter() {
    const counter = document.getElementById('imageCounter');
    if (counter && allImages.length > 1) {
        counter.textContent = `${currentFullscreenIndex + 1} / ${allImages.length}`;
        counter.style.display = 'block';
    } else if (counter) {
        counter.style.display = 'none';
    }
}

