// Admin Index Page Scripts

// Delete Modal Functionality
document.addEventListener('DOMContentLoaded', function() {
    var deleteModal = document.getElementById('deleteModal');
    if (deleteModal) {
        deleteModal.addEventListener('show.bs.modal', function (event) {
            // Button that triggered the modal
            var button = event.relatedTarget;
            
            // Extract info from data-bs-* attributes
            var propertyId = button.getAttribute('data-property-id');
            var propertyTitle = button.getAttribute('data-property-title');
            
            // Update the modal's content
            var modalTitle = deleteModal.querySelector('#propertyTitleText');
            var deleteForm = deleteModal.querySelector('#deleteForm');
            
            modalTitle.textContent = '"' + propertyTitle + '"';
            deleteForm.action = deleteFormActionUrl + '/' + propertyId;
        });
    }
});

// Favourite button functionality for Admin Panel
async function toggleFavouriteAdmin(button, propertyId) {
    if (!button || !propertyId) return;

    const icon = button.querySelector('i');
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    
    if (!tokenInput) {
        alert('Security token not found. Please refresh the page.');
        return;
    }

    const token = tokenInput.value;

    try {
        const formData = new URLSearchParams();
        formData.append('propertyId', propertyId);
        formData.append('__RequestVerificationToken', token);

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
            if (result.isFavourite) {
                icon.classList.remove('far');
                icon.classList.add('fas');
                button.style.color = '#fe5658';
            } else {
                icon.classList.remove('fas');
                icon.classList.add('far');
                button.style.color = '#6b7280';
            }
        } else {
            alert(result.message || 'An error occurred. Please try again.');
        }
    } catch (error) {
        console.error('Error toggling favourite:', error);
        alert('An error occurred. Please try again.');
    }
}

