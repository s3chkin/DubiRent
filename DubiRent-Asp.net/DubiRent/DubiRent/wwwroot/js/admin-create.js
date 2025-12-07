// Admin Create Property Page JavaScript

let selectedImages = [];
let mainImageIndex = 0;

const uploadZone = document.getElementById('uploadZone');
const fileInput = document.getElementById('fileInput');
const imagePreviewContainer = document.getElementById('imagePreviewContainer');
const mainImageIndexInput = document.getElementById('mainImageIndex');

// Click on upload zone
if (uploadZone) {
    uploadZone.addEventListener('click', () => {
        if (fileInput) {
            fileInput.click();
        }
    });
}

// File input change
if (fileInput) {
    fileInput.addEventListener('change', (e) => {
        handleFiles(e.target.files);
    });
}

// Drag and drop events
if (uploadZone) {
    uploadZone.addEventListener('dragover', (e) => {
        e.preventDefault();
        uploadZone.classList.add('drag-over');
    });

    uploadZone.addEventListener('dragleave', () => {
        uploadZone.classList.remove('drag-over');
    });

    uploadZone.addEventListener('drop', (e) => {
        e.preventDefault();
        uploadZone.classList.remove('drag-over');
        handleFiles(e.dataTransfer.files);
    });
}

function showErrorModal(message) {
    const modal = document.getElementById('errorModal');
    const modalBody = document.getElementById('errorModalBody');
    if (modal && modalBody) {
        // Format message with line breaks for multiple errors
        modalBody.innerHTML = message.split('\n').map(line => `<p style="margin-bottom: 0.5rem;">${line}</p>`).join('');
        modal.classList.add('show');
    }
}

function closeErrorModal() {
    const modal = document.getElementById('errorModal');
    if (modal) {
        modal.classList.remove('show');
    }
}

// Close modal on outside click
document.addEventListener('DOMContentLoaded', function() {
    const errorModal = document.getElementById('errorModal');
    if (errorModal) {
        errorModal.addEventListener('click', function(e) {
            if (e.target === this) {
                closeErrorModal();
            }
        });
    }

    // Close modal on Escape key
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape') {
            closeErrorModal();
        }
    });

    // Hide loading overlay on page load (in case form was returned with errors)
    const loadingOverlay = document.getElementById('formLoadingOverlay');
    if (loadingOverlay) {
        loadingOverlay.classList.remove('active');
    }
});

function handleFiles(files) {
    if (!files || files.length === 0) return;
    
    Array.from(files).forEach((file, index) => {
        if (file.type.startsWith('image/')) {
            // Check file size (5MB limit)
            if (file.size > 5 * 1024 * 1024) {
                showErrorModal(`File "${file.name}" exceeds the maximum file size of 5MB.`);
                return;
            }
            
            const reader = new FileReader();
            reader.onload = (e) => {
                const imageData = {
                    file: file,
                    preview: e.target.result,
                    index: selectedImages.length
                };
                selectedImages.push(imageData);
                renderImagePreview(imageData);
                updateFileInput();
                
                // Set first image as main by default
                if (selectedImages.length === 1) {
                    setMainImage(0);
                }
            };
            reader.readAsDataURL(file);
        }
    });
}

function renderImagePreview(imageData) {
    if (!imagePreviewContainer) return;
    
    const previewItem = document.createElement('div');
    previewItem.className = 'image-preview-item';
    previewItem.dataset.index = imageData.index;
    
    previewItem.innerHTML = `
        <img src="${imageData.preview}" alt="Preview" />
        <div class="image-preview-actions">
            <button type="button" class="image-preview-btn image-remove-btn" onclick="removeImage(${imageData.index})">
                <i class="fas fa-times"></i>
            </button>
        </div>
        <div class="image-main-badge">Main</div>
    `;

    previewItem.addEventListener('click', (e) => {
        if (!e.target.closest('.image-remove-btn')) {
            setMainImage(imageData.index);
        }
    });

    imagePreviewContainer.appendChild(previewItem);
}

function setMainImage(index) {
    mainImageIndex = index;
    if (mainImageIndexInput) {
        mainImageIndexInput.value = index;
    }
    
    // Update visual selection
    document.querySelectorAll('.image-preview-item').forEach((item, i) => {
        if (i === index) {
            item.classList.add('selected');
        } else {
            item.classList.remove('selected');
        }
    });
}

function removeImage(index) {
    // Remove from array
    selectedImages = selectedImages.filter((img, i) => i !== index);

    // Re-render all previews with updated indices
    if (imagePreviewContainer) {
        imagePreviewContainer.innerHTML = '';
        selectedImages.forEach((img, i) => {
            img.index = i;
            renderImagePreview(img);
        });
    }

    // Update main image if needed
    if (selectedImages.length > 0) {
        if (index === mainImageIndex) {
            // Main image was removed, set first as main
            setMainImage(0);
        } else if (index < mainImageIndex) {
            // Adjust main image index
            mainImageIndex--;
            setMainImage(mainImageIndex);
        } else {
            // Update visual selection for remaining items
            setMainImage(mainImageIndex);
        }
    } else {
        mainImageIndex = 0;
        if (mainImageIndexInput) {
            mainImageIndexInput.value = 0;
        }
    }

    // Update file input
    updateFileInput();
}

function updateFileInput() {
    if (!fileInput) return;
    
    const dt = new DataTransfer();
    selectedImages.forEach(img => {
        dt.items.add(img.file);
    });
    fileInput.files = dt.files;
}

// Update file input before form submit
document.addEventListener('DOMContentLoaded', function() {
    const propertyForm = document.getElementById('propertyForm');
    const loadingOverlay = document.getElementById('formLoadingOverlay');
    
    // Hide loading overlay on page load (in case form was returned with errors)
    if (loadingOverlay) {
        loadingOverlay.classList.remove('active');
    }
    
    if (propertyForm) {
        propertyForm.addEventListener('submit', function(e) {
            // FIRST: Hide overlay to ensure it's not showing
            if (loadingOverlay) {
                loadingOverlay.classList.remove('active');
            }
            
            updateFileInput();
            
            // Validate that at least one image is selected
            if (selectedImages.length === 0) {
                e.preventDefault();
                showErrorModal('Please select at least one image for the property.');
                return false;
            }
            
            // Check HTML5 validation BEFORE showing overlay
            // This prevents overlay from showing if form is invalid
            if (!propertyForm.checkValidity()) {
                // Form is invalid - don't show overlay, let browser show validation messages
                e.preventDefault();
                propertyForm.reportValidity();
                return false;
            }
            
            // Form is valid - NOW show loading overlay
            if (loadingOverlay) {
                loadingOverlay.classList.add('active');
            }
        });
    }
});

// Make functions globally available for onclick handlers and inline Razor scripts
window.removeImage = removeImage;
window.closeErrorModal = closeErrorModal;
window.showErrorModal = showErrorModal;

