// Loading indicator utility functions

// Show loading spinner on button
function showButtonLoading(button, text = null) {
    if (!button || button.disabled) return;
    
    const originalContent = button.innerHTML;
    let loadingText = text;
    
    if (!loadingText) {
        // Extract text content without icons
        const tempDiv = document.createElement('div');
        tempDiv.innerHTML = originalContent;
        loadingText = tempDiv.textContent.trim() || 'Loading...';
    }
    
    button.disabled = true;
    button.dataset.originalContent = originalContent;
    button.innerHTML = `
        <span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
        ${loadingText}
    `;
}

// Hide loading spinner on button
function hideButtonLoading(button) {
    if (!button || !button.dataset.originalContent) return;
    
    button.innerHTML = button.dataset.originalContent;
    button.disabled = false;
    delete button.dataset.originalContent;
}

// Show loading overlay on form
function showFormLoading(form) {
    if (!form) return;
    
    form.classList.add('form-loading');
    
    // Disable all inputs and buttons except cancel buttons
    const inputs = form.querySelectorAll('input:not([type="button"]):not(.btn-cancel), select, textarea, button[type="submit"]');
    inputs.forEach(input => {
        input.disabled = true;
    });
    
    // Show loading indicator
    let loadingOverlay = form.querySelector('.form-loading-overlay');
    if (!loadingOverlay) {
        loadingOverlay = document.createElement('div');
        loadingOverlay.className = 'form-loading-overlay';
        loadingOverlay.innerHTML = `
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Loading...</span>
            </div>
        `;
        const formStyle = window.getComputedStyle(form);
        if (formStyle.position === 'static') {
            form.style.position = 'relative';
        }
        form.appendChild(loadingOverlay);
    }
    loadingOverlay.style.display = 'flex';
}

// Hide loading overlay on form
function hideFormLoading(form) {
    if (!form) return;
    
    form.classList.remove('form-loading');
    
    // Enable all inputs and buttons
    const inputs = form.querySelectorAll('input, select, textarea, button');
    inputs.forEach(input => {
        input.disabled = false;
    });
    
    // Hide loading indicator
    const loadingOverlay = form.querySelector('.form-loading-overlay');
    if (loadingOverlay) {
        loadingOverlay.style.display = 'none';
    }
}

// Initialize loading indicators on all forms
document.addEventListener('DOMContentLoaded', function() {
    // Prevent double submission - use a Map to track each form separately
    const submittingForms = new Map();
    
    // Add loading to all forms on submit
    document.querySelectorAll('form').forEach(form => {
        // Skip inline forms (like action buttons in viewing requests) - they have their own handler
        const formStyle = form.getAttribute('style') || '';
        if (formStyle.includes('display: inline') || formStyle.includes('display:inline') || form.classList.contains('inline-form')) {
            return;
        }
        
        form.addEventListener('submit', function(e) {
            // Check if already submitting
            if (submittingForms.get(form)) {
                e.preventDefault();
                return false;
            }
            
            // Don't show loading if form validation fails
            if (form.noValidate || this.checkValidity()) {
                const submitButton = form.querySelector('button[type="submit"]');
                if (submitButton && !submitButton.dataset.noLoading) {
                    showButtonLoading(submitButton);
                } else {
                    showFormLoading(form);
                }
                
                submittingForms.set(form, true);
                
                // Reset flag after 5 seconds (in case of validation errors or network issues)
                setTimeout(() => {
                    submittingForms.delete(form);
                }, 5000);
            }
        }, false);
    });
    
    // Add loading to delete form specifically
    const deleteForm = document.querySelector('#deleteForm');
    if (deleteForm) {
        deleteForm.addEventListener('submit', function(e) {
            const submitBtn = this.querySelector('button[type="submit"]');
            if (submitBtn) {
                showButtonLoading(submitBtn, 'Deleting...');
                // Also close modal after showing loading (it will redirect anyway)
                const modal = bootstrap.Modal.getInstance(document.getElementById('deleteModal'));
                if (modal) {
                    setTimeout(() => modal.hide(), 500);
                }
            }
        });
    }
    
    // Add loading to cancel request form in modal
    const cancelRequestForm = document.querySelector('#cancelRequestForm');
    if (cancelRequestForm) {
        cancelRequestForm.addEventListener('submit', function(e) {
            const submitBtn = this.querySelector('button[type="submit"]');
            if (submitBtn) {
                showButtonLoading(submitBtn, 'Cancelling...');
            }
        });
    }
    
    // Handle inline forms (viewing requests) separately - prevent double submission only
    document.querySelectorAll('form[style*="display: inline"]').forEach(form => {
        form.addEventListener('submit', function(e) {
            // Check if already submitting
            if (submittingForms.get(form)) {
                e.preventDefault();
                return false;
            }
            
            // Mark as submitting to prevent double submission
            submittingForms.set(form, true);
            
            // Reset flag after 5 seconds in case of issues (allows retry if needed)
            setTimeout(() => {
                submittingForms.delete(form);
            }, 5000);
        }, true); // Use capture phase
    });
});
