$(document).ready(function () {
    // Show loading overlay and success message if exists
    const successMessage = $('#successMessageData').data('value');
    if (successMessage) {
        $('#successMessage').text(successMessage);
        $('#loadingOverlay').addClass('active');
        setTimeout(function () {
            $('#loadingOverlay').removeClass('active');
        }, 1000);
    }

    // Initialize DataTable
    $('#supplierTable').DataTable({
        "responsive": true,
        "lengthChange": false,
        "autoWidth": false,
        "order": [[0, 'desc']],
        "language": {
            "search": "",
            "searchPlaceholder": "Search requests..."
        }
    });

    // Modernize DataTable Search
    $('.dataTables_filter input').addClass('form-control').css({
        'border-radius': '10px',
        'padding': '10px 15px',
        'width': '250px'
    });
});

// Create Logic
(function () {
    let items = [];
    const form = $('#createSupplierForm');
    const itemsBody = $('#newSupplierItemsBody');
    const inputArea = $('#newSupplierLineItemInputs');
    const summaryTextarea = $('#newSupplierProductsAndQuantities');
    const addBtn = $('#addSupplierItemBtn');

    addBtn.on('click', function() {
        const name = $('#newSupplierProductName').val().trim();
        const qty = parseInt($('#newSupplierQuantity').val());
        if (!name || isNaN(qty) || qty < 1) return;

        const existing = items.find(i => i.productName.toLowerCase() === name.toLowerCase());
        if (existing) {
            existing.quantity += qty;
        } else {
            items.push({ productName: name, quantity: qty });
        }

        $('#newSupplierProductName').val('').focus();
        $('#newSupplierQuantity').val('');
        renderItems();
    });

    function renderItems() {
        itemsBody.empty();
        inputArea.empty();
        let summaryLines = ["Product Name | Quantity"];

        items.forEach((item, index) => {
            itemsBody.append(`
                <tr>
                    <td class="fw-bold">${item.productName}</td>
                    <td>${item.quantity}</td>
                    <td class="text-end">
                        <button type="button" class="btn btn-link text-danger p-0" onclick="removeItem(${index})">
                            <i class="fas fa-times-circle"></i>
                        </button>
                    </td>
                </tr>
            `);
            inputArea.append(`<input type="hidden" name="LineItems[${index}].ProductName" value="${item.productName}">`);
            inputArea.append(`<input type="hidden" name="LineItems[${index}].Quantity" value="${item.quantity}">`);
            summaryLines.push(`${item.productName} | ${item.quantity}`);
        });
        summaryTextarea.val(summaryLines.join('\n'));
    }

    window.removeItem = function(index) {
        items.splice(index, 1);
        renderItems();
    };

    form.on('submit', async function(e) {
        e.preventDefault();
        if (items.length === 0) return alert("Add at least one item.");
        const formData = new FormData(this);
        const url = $(this).attr('action');

        $.ajax({
            url: url,
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            headers: { 'X-Requested-With': 'XMLHttpRequest' },
            success: function() { location.reload(); },
            error: function(xhr) { alert(xhr.responseJSON?.message || "Error saving"); }
        });
    });
})();

// Edit Logic
(function () {
    let editItems = [];
    const editForm = $('#editSupplierForm');
    const editItemsBody = $('#editSupplierItemsBody');
    const editInputArea = $('#editSupplierLineItemInputs');
    const editSummaryTextarea = $('#editSupplierProductsAndQuantities');

    $('.edit-supplier-btn').on('click', async function() {
        const id = $(this).data('supplier-id');
        const url = $('#metadata').data('get-edit-url') + `?id=${id}`;
        
        const response = await fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
        const result = await response.json();
        if (result.success) {
            const s = result.data;
            $('#editSupplierId').val(s.supplierId);
            $('#editSupplierStatus').val(s.status);
            $('#editSupplierName').val(s.supplierName);
            $('#editSupplierEmail').val(s.email);
            $('#editSupplierUnitPrice').val(s.unitPrice);
            $('#editSupplierDescription').val(s.description);
            
            editItems = s.lineItems || [];
            renderEditItems();
            $('#editSupplierModal').modal('show');
        }
    });

    $('#editAddSupplierItemBtn').on('click', function() {
        const name = $('#editSupplierProductName').val().trim();
        const qty = parseInt($('#editSupplierQuantity').val());
        if (!name || isNaN(qty) || qty < 1) return;

        const existing = editItems.find(i => i.productName.toLowerCase() === name.toLowerCase());
        if (existing) {
            existing.quantity += qty;
        } else {
            editItems.push({ productName: name, quantity: qty });
        }

        $('#editSupplierProductName').val('').focus();
        $('#editSupplierQuantity').val('');
        renderEditItems();
    });

    function renderEditItems() {
        editItemsBody.empty();
        editInputArea.empty();
        let summaryLines = ["Product Name | Quantity"];

        editItems.forEach((item, index) => {
            editItemsBody.append(`
                <tr>
                    <td class="fw-bold">${item.productName}</td>
                    <td>${item.quantity}</td>
                    <td class="text-end">
                        <button type="button" class="btn btn-link text-danger p-0" onclick="removeEditItem(${index})">
                            <i class="fas fa-times-circle"></i>
                        </button>
                    </td>
                </tr>
            `);
            editInputArea.append(`<input type="hidden" name="LineItems[${index}].ProductName" value="${item.productName}">`);
            editInputArea.append(`<input type="hidden" name="LineItems[${index}].Quantity" value="${item.quantity}">`);
            summaryLines.push(`${item.productName} | ${item.quantity}`);
        });
        editSummaryTextarea.val(summaryLines.join('\n'));
    }

    window.removeEditItem = function(index) {
        editItems.splice(index, 1);
        renderEditItems();
    };

    editForm.on('submit', async function(e) {
        e.preventDefault();
        const id = $('#editSupplierId').val();
        const formData = new FormData(this);
        $.ajax({
            url: `${$(this).attr('action')}/${id}`,
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            headers: { 'X-Requested-With': 'XMLHttpRequest' },
            success: function() { location.reload(); }
        });
    });
})();

function filterSuppliers() {
    const val = $('#statusFilter').val();
    const table = $('#supplierTable').DataTable();
    if (val === 'all') {
        table.column(5).search('').draw();
    } else {
        table.column(5).search(val, true, false).draw();
    }
}
