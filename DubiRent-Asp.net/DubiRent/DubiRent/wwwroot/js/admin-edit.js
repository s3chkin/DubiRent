// Admin Edit Property Page JavaScript

var pendingDeleteImageId = null;
var pendingDeleteButton = null;

function showDeleteImageModal(imageId, button, imageUrl) {
    pendingDeleteImageId = imageId;
    pendingDeleteButton = button;
    
    // Show preview image
    var previewImage = document.getElementById('previewImage');
    if (previewImage) {
        previewImage.src = imageUrl;
    }
    
    // Show modal
    var modalElement = document.getElementById('deleteImageModal');
    if (modalElement) {
        var modal = new bootstrap.Modal(modalElement);
        modal.show();
    }
}

function markImageForDeletion(imageId, button) {
    const hiddenInput = document.getElementById('img-' + imageId);
    const imageContainer = button.closest('.existing-image');
    const radioInput = document.getElementById('main-' + imageId);
    
    if (!hiddenInput || !imageContainer) return;
    
    if (hiddenInput.value === '') {
        // Mark for deletion
        hiddenInput.value = imageId;
        imageContainer.style.opacity = '0.5';
        imageContainer.style.border = '2px solid #dc2626';
        button.style.background = 'rgba(34, 197, 94, 0.9)';
        button.innerHTML = '<i class="fas fa-undo"></i>';
        // Uncheck radio if it's marked for deletion
        if (radioInput) {
            radioInput.checked = false;
            radioInput.disabled = true;
        }
        imageContainer.classList.remove('selected');
    } else {
        // Unmark
        hiddenInput.value = '';
        imageContainer.style.opacity = '1';
        imageContainer.style.border = '2px solid transparent';
        button.style.background = 'rgba(220, 38, 38, 0.9)';
        button.innerHTML = '<i class="fas fa-times"></i>';
        if (radioInput) {
            radioInput.disabled = false;
        }
    }
}

function selectMainImage(imageId) {
    const imageContainer = event.currentTarget;
    const radioInput = document.getElementById('main-' + imageId);
    const hiddenInput = document.getElementById('img-' + imageId);
    
    // Don't allow selecting if marked for deletion
    if (hiddenInput && hiddenInput.value !== '') {
        return;
    }

    // Uncheck all other radios
    document.querySelectorAll('input[name="MainImageId"]').forEach(radio => {
        radio.checked = false;
        const container = radio.closest('.existing-image');
        if (container) {
            container.classList.remove('selected');
            const badge = container.querySelector('.image-main-badge');
            if (badge) badge.remove();
        }
    });

    // Check this radio
    if (radioInput) {
        radioInput.checked = true;
        imageContainer.classList.add('selected');
        
        // Add or update main badge
        let badge = imageContainer.querySelector('.image-main-badge');
        if (!badge) {
            badge = document.createElement('span');
            badge.className = 'image-main-badge';
            badge.textContent = 'Main';
            imageContainer.appendChild(badge);
        }
    }
}

// Initialize selected state on page load
document.addEventListener('DOMContentLoaded', function() {
    // Handle confirm delete button
    var confirmDeleteBtn = document.getElementById('confirmDeleteImageBtn');
    if (confirmDeleteBtn) {
        confirmDeleteBtn.addEventListener('click', function() {
            if (pendingDeleteImageId && pendingDeleteButton) {
                markImageForDeletion(pendingDeleteImageId, pendingDeleteButton);
                var modalElement = document.getElementById('deleteImageModal');
                if (modalElement) {
                    var modal = bootstrap.Modal.getInstance(modalElement);
                    if (modal) {
                        modal.hide();
                    }
                }
                pendingDeleteImageId = null;
                pendingDeleteButton = null;
            }
        });
    }

    // Initialize selected state
    document.querySelectorAll('input[name="MainImageId"]:checked').forEach(radio => {
        const container = radio.closest('.existing-image');
        if (container) {
            container.classList.add('selected');
        }
    });
});

