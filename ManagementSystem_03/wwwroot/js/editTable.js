$(document).ready(function () {
    $(".edit-btn").click(function () {
        var rowIndex = $(this).data('row-index');
        var rowData = {};
        var headers = [];
        $("#editFormContainer").empty();

        $("table thead th").each(function () {
            headers.push($(this).text());
        });
        $("table tbody tr").eq(rowIndex).find("td").each(function (index) {
            rowData[headers[index]] = $(this).text();
        });

        var editForm = $("<form id='editForm'>");
        var headersInput = $("<input>").attr("type", "hidden").attr("name", "headers").val(JSON.stringify(headers));
        editForm.prepend(headersInput);

        for (var header of headers) {
            var formGroup;
            var label;
            var input;

            if (header.toLowerCase() !== 'theindex' && header.toLowerCase() !== 'פעולות') {
                formGroup = $("<div>").addClass("form-group");
                label = $("<label>").text(header + ":");
                input = $("<input>").attr("type", "text").addClass("form-control").attr("name", header).val(rowData[header]);
                formGroup.append(label).append(input);
                editForm.append(formGroup);
            } else if (header.toLowerCase() === 'theindex') {
                formGroup = $("<div>").addClass("form-group");
                label = $("<label>").text(header + ":");
                input = $("<input>").attr("type", "text").addClass("form-control").attr("name", header).val(rowData[header]).prop("readonly", true);
                formGroup.append(label).append(input);
                editForm.append(formGroup);
            }
        }

        var saveButton = $("<button>").attr("type", "button").addClass("btn btn-primary").text("שמור").click(function () {
            console.log(selectedTableName);
            submitEditForm(selectedTableName); // השתמש במשתנה הגלובלי
        });
        var cancelButton = $("<button>").attr("type", "button").addClass("btn btn-secondary").text("בטל").click(function () {
            $("#editFormContainer").hide();
        });
        editForm.append(saveButton).append(cancelButton);

        $("#editFormContainer").append(editForm).show();
    });

    function submitEditForm(tableName) {
        var formData = $("#editForm").serialize();
        $.ajax({
            url: '/Tables/Edit/' + tableName,
            type: 'POST',
            data: formData,
            success: function (result) {
                if (result.success) {
                    location.reload();
                } else {
                    alert('שגיאה בעריכת הנתונים: ' + result.message);
                }
            },
            error: function (xhr, status, error) {
                alert('שגיאת AJAX: ' + error);
                $("#editFormContainer").hide();
            }
        });
    }
});