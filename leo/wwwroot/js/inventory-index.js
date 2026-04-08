// Inventory Management Scripts
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
    $('#inventoryTable').DataTable({
        "responsive": true,
        "lengthChange": false,
        "autoWidth": false,
        "dom": '<"top"f>rt<"bottom"ip><"clear">',
        "language": {
            "search": "",
            "searchPlaceholder": "Search inventory..."
        }
    });

    // Modernize DataTable Search
    $('.dataTables_filter input').addClass('form-control').css({
        'border-radius': '10px',
        'padding': '10px 15px',
        'width': '250px'
    });

    // Load categories for create modal
    $('#createInventoryModal').on('show.bs.modal', function () {
        loadCategorySelect('#categoryId');
    });

    // Populate Edit Modal
    $('.edit-inventory-btn').on('click', function () {
        const data = $(this).data();
        $('#editId').val(data.productId);
        $('#editProductId').val(data.productId);
        $('#editProductName').val(data.productName);
        $('#editBarcode').val(data.barcode);
        $('#editDate').val(data.date);
        $('#editUnitPrice').val(data.unitPrice);
        $('#editStockQuantity').val(data.stockQuantity);
        $('#editDescription').val(data.description);
        $('#editSuppliers').val(data.suppliers);
        $('#editImagePath').val(data.imagePath);

        loadCategorySelect('#editCategoryId', data.categoryId);
        $('#editInventoryModal').modal('show');
    });

    // Manage Categories Loading
    $('#manageCategoriesModal').on('show.bs.modal', function () {
        loadCategoriesTable();
    });
});

function loadCategorySelect(selector, selectedValue) {
    const url = $('#metadata').data('get-categories-url');
    $.ajax({
        url: url,
        type: 'GET',
        dataType: 'json',
        success: function (categories) {
            const select = $(selector);
            select.empty();
            select.append('<option value="">Select a Category</option>');
            if (categories && categories.length > 0) {
                categories.forEach(function (category) {
                    const id = category.CategoryId || category.categoryId;
                    const name = category.CategoryName || category.categoryName;
                    const selected = (id == selectedValue) ? 'selected' : '';
                    select.append(`<option value="${id}" ${selected}>${name}</option>`);
                });
            }
        }
    });
}

function loadCategoriesTable() {
    const url = $('#metadata').data('get-categories-url');
    $.ajax({
        url: url,
        type: 'GET',
        dataType: 'json',
        success: function (categories) {
            const tbody = $('#categoriesTableBody');
            tbody.empty();
            if (!categories || categories.length === 0) {
                tbody.append('<tr><td colspan="2" class="text-center text-muted">No categories found</td></tr>');
                return;
            }
            categories.forEach(function (category) {
                const id = category.CategoryId || category.categoryId;
                const name = category.CategoryName || category.categoryName;
                tbody.append(`
                    <tr>
                        <td class="fw-bold text-dark">${name}</td>
                        <td class="text-end">
                            <button class="btn btn-light btn-sm rounded-pill px-3" onclick="editCategory(${id}, '${name}')">
                                <i class="fas fa-edit text-primary"></i>
                            </button>
                            <button class="btn btn-light btn-sm rounded-pill px-3" onclick="showDeleteCategoryModal(${id})">
                                <i class="fas fa-trash text-danger"></i>
                            </button>
                        </td>
                    </tr>
                `);
            });
        }
    });
}

// Category Actions
function createCategory() {
    const categoryName = $('#categoryNameInput').val().trim();
    if (!categoryName) return;
    const url = $('#metadata').data('create-category-url');
    const token = $('input[name="__RequestVerificationToken"]').val();

    $.post(url, { categoryName, __RequestVerificationToken: token }, function (response) {
        if (response.success) {
            $('#categoryNameInput').val('');
            loadCategoriesTable();
        }
    });
}

function editCategory(id, name) {
    $('#editCategoryId').val(id);
    $('#editCategoryName').val(name);
    new bootstrap.Modal(document.getElementById('editCategoryModal')).show();
}

function saveCategory() {
    const id = $('#editCategoryId').val();
    const categoryName = $('#editCategoryName').val().trim();
    const url = $('#metadata').data('edit-category-url');
    const token = $('input[name="__RequestVerificationToken"]').val();

    $.post(url, { id, categoryName, __RequestVerificationToken: token }, function (response) {
        if (response.success) {
            bootstrap.Modal.getInstance(document.getElementById('editCategoryModal')).hide();
            loadCategoriesTable();
        }
    });
}

let currentDeleteCategoryId = null;
function showDeleteCategoryModal(id) {
    currentDeleteCategoryId = id;
    new bootstrap.Modal(document.getElementById('deleteCategoryModal')).show();
}

function confirmDeleteCategory() {
    const url = $('#metadata').data('delete-category-url');
    const token = $('input[name="__RequestVerificationToken"]').val();

    $.post(url, { id: currentDeleteCategoryId, __RequestVerificationToken: token }, function (response) {
        if (response.success) {
            bootstrap.Modal.getInstance(document.getElementById('deleteCategoryModal')).hide();
            loadCategoriesTable();
        }
    });
}

// Custom Filters
function filterProductsByDate() {
    const filter = $('#sortFilter').val();
    const table = $('#inventoryTable').DataTable();
    const rows = table.rows().data().toArray();
    
    // DataTables sorting is better handled via API
    if (filter === 'latest') {
        table.order([5, 'desc']).draw();
    } else if (filter === 'oldest') {
        table.order([5, 'asc']).draw();
    }
}

function filterProductsByStock() {
    const filter = $('#stockFilter').val();
    const table = $('#inventoryTable').DataTable();
    
    $.fn.dataTable.ext.search.push(function(settings, data, dataIndex) {
        const qty = parseInt(data[7]) || 0;
        if (filter === 'all') return true;
        if (filter === 'in-stock') return qty > 10;
        if (filter === 'low-stock') return qty <= 10 && qty > 0;
        if (filter === 'out-of-stock') return qty === 0;
        return true;
    });
    
    table.draw();
    $.fn.dataTable.ext.search.pop();
}

// Global Item Actions
function createInventoryItem() {
    const form = $('#createInventoryForm')[0];
    const formData = new FormData(form);
    const url = $('#metadata').data('create-item-url');
    const token = $('input[name="__RequestVerificationToken"]').val();
    formData.append('__RequestVerificationToken', token);

    $.ajax({
        url: url,
        type: 'POST',
        data: formData,
        processData: false,
        contentType: false,
        success: function (response) {
            if (response.success) {
                location.reload();
            } else {
                alert(response.message || "Error creating item");
            }
        }
    });
}

function deleteInventoryItem(event) {
    event.preventDefault();
    const form = event.target.closest('form');
    new bootstrap.Modal(document.getElementById('deleteInventoryModal')).show();
    document.getElementById('confirmDeleteButton').onclick = function () {
        form.submit();
    };
}
