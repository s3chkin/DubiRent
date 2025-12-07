// Admin Viewing Requests Page JavaScript

document.addEventListener('DOMContentLoaded', function() {
    // Cancel Request Modal
    var cancelRequestModal = document.getElementById('cancelRequestModal');
    if (cancelRequestModal) {
        cancelRequestModal.addEventListener('show.bs.modal', function (event) {
            // Button that triggered the modal
            var button = event.relatedTarget;
            
            // Extract info from data-bs-* attributes
            var requestId = button.getAttribute('data-request-id');
            var propertyTitle = button.getAttribute('data-property-title');
            var requestDate = button.getAttribute('data-request-date');
            
            // Update the modal's content
            var modalPropertyTitle = cancelRequestModal.querySelector('#requestPropertyTitle');
            var modalRequestDate = cancelRequestModal.querySelector('#requestDate');
            var cancelForm = cancelRequestModal.querySelector('#cancelRequestForm');
            
            if (modalPropertyTitle) {
                modalPropertyTitle.textContent = '"' + propertyTitle + '"';
            }
            if (modalRequestDate) {
                modalRequestDate.textContent = requestDate;
            }
            if (cancelForm) {
                // Get the UpdateRequestStatus URL from the form's data attribute or construct it
                var updateStatusUrl = cancelForm.getAttribute('data-action-url') || '/Admin/UpdateRequestStatus';
                if (updateStatusUrl) {
                    cancelForm.action = updateStatusUrl;
                }
                
                // Update the hidden input for request ID
                var existingIdInput = cancelForm.querySelector('input[name="id"]');
                if (existingIdInput) {
                    existingIdInput.value = requestId;
                } else {
                    var idInput = document.createElement('input');
                    idInput.type = 'hidden';
                    idInput.name = 'id';
                    idInput.value = requestId;
                    cancelForm.appendChild(idInput);
                }
            }
        });
    }

    // Loading modal for all status update forms
    var loadingModalElement = document.getElementById('loadingModal');
    var loadingModal = null;
    var formsInitialized = false;

    // Initialize Bootstrap Modal
    function initLoadingModal() {
        if (!loadingModalElement) {
            return;
        }

        // Check if Bootstrap is available
        if (typeof bootstrap !== 'undefined' && bootstrap.Modal) {
            try {
                loadingModal = new bootstrap.Modal(loadingModalElement, {
                    backdrop: 'static',
                    keyboard: false
                });
                
                // Handle all forms that update request status (approve, complete, etc.)
                if (!formsInitialized) {
                    var statusUpdateForms = document.querySelectorAll('form[action*="UpdateRequestStatus"], .approve-form');
                    statusUpdateForms.forEach(function(form) {
                        // Remove any existing submit listeners by cloning the form
                        var newForm = form.cloneNode(true);
                        form.parentNode.replaceChild(newForm, form);
                        
                        newForm.addEventListener('submit', function(e) {
                            // Show loading modal immediately
                            if (loadingModal) {
                                try {
                                    loadingModal.show();
                                } catch (err) {
                                    console.error('Error showing loading modal:', err);
                                    // Fallback: show modal using jQuery if available
                                    if (typeof $ !== 'undefined' && $.fn.modal) {
                                        $(loadingModalElement).modal({ backdrop: 'static', keyboard: false });
                                    }
                                }
                            }
                        });
                    });
                    formsInitialized = true;
                }
            } catch (err) {
                console.error('Error initializing loading modal:', err);
                // Retry after a delay
                setTimeout(initLoadingModal, 200);
            }
        } else {
            // Bootstrap not loaded yet, wait and try again
            setTimeout(initLoadingModal, 100);
        }
    }

    // Initialize on DOM ready
    initLoadingModal();
});

// Also try to initialize on window load (in case DOMContentLoaded fires too early)
window.addEventListener('load', function() {
    var loadingModalElement = document.getElementById('loadingModal');
    if (loadingModalElement && typeof bootstrap !== 'undefined' && bootstrap.Modal) {
        var loadingModal = new bootstrap.Modal(loadingModalElement, {
            backdrop: 'static',
            keyboard: false
        });
        
        var statusUpdateForms = document.querySelectorAll('form[action*="UpdateRequestStatus"], .approve-form');
        statusUpdateForms.forEach(function(form) {
            form.addEventListener('submit', function(e) {
                if (loadingModal) {
                    loadingModal.show();
                }
            });
        });
    }
});

