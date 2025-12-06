// Prevent double submission on forms
document.addEventListener('DOMContentLoaded', function() {
    // Prevent double submission - use a Map to track each form separately
    const submittingForms = new Map();
    
    // Add double submission prevention to all forms on submit
    document.querySelectorAll('form').forEach(form => {
        // Skip external login form - it redirects to external provider and should not be blocked
        if (form.id === 'external-account' || form.action?.includes('ExternalLogin')) {
            return;
        }
        
        form.addEventListener('submit', function(e) {
            // Check if already submitting
            if (submittingForms.get(form)) {
                e.preventDefault();
                return false;
            }
            
            // Don't prevent submission if form validation fails
            if (form.noValidate || this.checkValidity()) {
                submittingForms.set(form, true);
                
                // Reset flag after 5 seconds (in case of validation errors or network issues)
                setTimeout(() => {
                    submittingForms.delete(form);
                }, 5000);
            }
        }, false);
    });
});
