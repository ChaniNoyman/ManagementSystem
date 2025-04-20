document.addEventListener('click', function (event) {
    if (event.target.classList.contains('delete-button')) {
        console.log('delete button clicked');
        const id = event.target.getAttribute('data-id');
        const tableName = event.target.getAttribute('data-table-name');

        console.log('tableName:', tableName, 'id:', id);

        fetch('/TablesController/DeletePost', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ tableName: tableName, id: id })
        })
            .then(response => {
                if (response.ok) {
                    // �����
                    console.log('Record deleted successfully');
                    // ���� �� �����
                    location.reload();
                } else {
                    // �����
                    console.error('Error deleting record:', response.statusText);
                }
            })
            .catch(error => {
                console.error('Network error:', error);
            });
    }
});