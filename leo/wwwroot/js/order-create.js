(function () {
    const config = window.orderCreateConfig;

    if (!config) {
        return;
    }

    const state = {
        cart: [],
        products: normalizeProducts(config.initialProducts || []),
        lastInvoice: null,
        isSubmitting: false,
        currentCategory: 'all'
    };

    document.addEventListener('DOMContentLoaded', function () {
        bindEvents();
        renderProducts();
        renderCart();
        updateTime();
        window.setInterval(updateTime, 1000);
        loadProducts();
    });

    function bindEvents() {
        const searchInput = document.getElementById('searchProducts');
        const productsGrid = document.getElementById('productsGrid');

        if (searchInput) {
            searchInput.addEventListener('input', function () {
                filterProducts(searchInput.value);
            });
        }

        if (productsGrid) {
            productsGrid.addEventListener('click', function (event) {
                const card = event.target.closest('.product-card');
                if (!card) {
                    return;
                }

                const productId = card.dataset.productId || '';
                const productName = card.dataset.productName || 'Product';
                const productPrice = parseFloat(card.dataset.productPrice || '0');

                if (!productId) {
                    return;
                }

                // Add animation effect
                card.style.transform = 'scale(0.95)';
                setTimeout(() => card.style.transform = '', 100);

                addToCart(productId, productName, productPrice);
            });
        }

        // Category filter buttons
        const categoryButtons = document.querySelectorAll('.btn-category');
        categoryButtons.forEach(btn => {
            btn.addEventListener('click', function() {
                const category = this.dataset.category;
                filterByCategory(category);
            });
        });
    }

    function filterByCategory(category) {
        // Update active state of buttons
        document.querySelectorAll('.btn-category').forEach(btn => {
            btn.classList.remove('active');
            btn.style.background = 'white';
            btn.style.color = '#1e293b';
            btn.style.borderColor = '#e2e8f0';
        });

        const activeBtn = document.querySelector(`.btn-category[data-category="${category}"]`);
        if (activeBtn) {
            activeBtn.style.background = '#0f766e';
            activeBtn.style.color = 'white';
            activeBtn.style.borderColor = '#0f766e';
            activeBtn.classList.add('active');
        }

        const cards = document.querySelectorAll('.product-card');
        cards.forEach(card => {
            const matchesCategory = category === 'all' || card.dataset.category === category;
            const matchesSearch = doesMatchSearch(card);
            
            if (matchesCategory && matchesSearch) {
                card.style.display = 'flex';
                card.style.animation = 'fadeIn 0.3s ease-out forwards';
            } else {
                card.style.display = 'none';
            }
        });
        
        state.currentCategory = category;
    }

    function doesMatchSearch(card) {
        const searchInput = document.getElementById('searchProducts');
        if (!searchInput) return true;
        const query = searchInput.value.toLowerCase();
        const productName = (card.dataset.productName || '').toLowerCase();
        const barcode = (card.dataset.barcode || '').toLowerCase();
        return productName.includes(query) || barcode.includes(query);
    }

    function loadProducts() {
        if (!config.getProductsUrl || !window.fetch) {
            return;
        }

        fetch(config.getProductsUrl, {
            method: 'GET',
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        })
            .then(function (response) {
                if (!response.ok) {
                    throw new Error('Unable to load products.');
                }

                return response.json();
            })
            .then(function (products) {
                const normalized = normalizeProducts(products || []);
                if (!normalized.length) {
                    return;
                }

                state.products = normalized;
                renderProducts();
            })
            .catch(function () {
                if (!state.products.length) {
                    const grid = document.getElementById('productsGrid');
                    if (grid) {
                        grid.innerHTML = '<div style="grid-column: 1/-1; padding: 40px; text-align: center; color: #6b7280;">Unable to load products</div>';
                    }
                }
            });
    }

    function normalizeProducts(products) {
        return (products || [])
            .map(function (product) {
                return {
                    id: String(product.ProductId || product.productId || product.id || ''),
                    name: String(product.ProductName || product.productName || product.name || 'Product'),
                    price: parseFloat(product.UnitPrice || product.unitPrice || product.price || 0),
                    stockQuantity: parseInt(product.StockQuantity || product.stockQuantity || 0, 10),
                    imagePath: String(product.ImagePath || product.imagePath || ''),
                    barcode: String(product.Barcode || product.barcode || '')
                };
            })
            .filter(function (product) {
                return product.id;
            });
    }

    function renderProducts() {
        const grid = document.getElementById('productsGrid');
        if (!grid) {
            return;
        }

        grid.innerHTML = '';

        if (!state.products.length) {
            grid.innerHTML = '<div style="grid-column: 1/-1; padding: 40px; text-align: center; color: #6b7280;">No products available</div>';
            return;
        }

        const icons = ['#', '*', '+', '@', '%', '&', '=', '?'];

        state.products.forEach(function (product, index) {
            const card = document.createElement('div');
            card.className = 'product-card';
            card.dataset.productId = product.id;
            card.dataset.productName = product.name;
            card.dataset.productPrice = String(product.price);
            card.dataset.barcode = product.barcode;
            card.setAttribute('role', 'button');
            card.setAttribute('tabindex', '0');

            const icon = document.createElement('div');
            icon.className = 'product-icon';

            if (product.imagePath) {
                const image = document.createElement('img');
                image.src = product.imagePath;
                image.alt = product.name;
                image.style.width = '100%';
                image.style.height = '100%';
                image.style.objectFit = 'cover';
                image.style.borderRadius = '8px';
                icon.appendChild(image);
            } else {
                icon.textContent = icons[index % icons.length];
            }

            const name = document.createElement('div');
            name.className = 'product-name';
            name.textContent = product.name;

            const price = document.createElement('div');
            price.className = 'product-price';
            price.textContent = formatCurrency(product.price);

            card.appendChild(icon);
            card.appendChild(name);
            card.appendChild(price);
            card.addEventListener('keydown', function (event) {
                if (event.key === 'Enter' || event.key === ' ') {
                    event.preventDefault();
                    addToCart(product.id, product.name, product.price);
                }
            });

            grid.appendChild(card);
        });

        filterProducts(document.getElementById('searchProducts')?.value || '');
    }

    function filterProducts(query) {
        const normalizedQuery = String(query || '').toLowerCase();
        const cards = document.querySelectorAll('#productsGrid .product-card');

        cards.forEach(function (card) {
            const name = (card.dataset.productName || '').toLowerCase();
            const barcode = (card.dataset.barcode || '').toLowerCase();
            const category = card.dataset.category || '';
            
            const matchesSearch = name.indexOf(normalizedQuery) !== -1 || barcode.indexOf(normalizedQuery) !== -1;
            const matchesCategory = !state.currentCategory || state.currentCategory === 'all' || category === state.currentCategory;

            if (matchesSearch && matchesCategory) {
                card.style.display = 'flex';
            } else {
                card.style.display = 'none';
            }
        });
    }

    function addToCart(productId, productName, price) {
        const existingItem = state.cart.find(function (item) {
            return item.id === String(productId);
        });

        if (existingItem) {
            existingItem.qty += 1;
        } else {
            state.cart.push({
                id: String(productId),
                name: String(productName || 'Product'),
                price: parseFloat(price || 0),
                qty: 1
            });
        }

        clearFeedback();
        renderCart();
    }

    function removeFromCart(productId) {
        state.cart = state.cart.filter(function (item) {
            return item.id !== String(productId);
        });

        renderCart();
    }

    function updateQty(productId, delta) {
        const item = state.cart.find(function (entry) {
            return entry.id === String(productId);
        });

        if (!item) {
            return;
        }

        item.qty = Math.max(1, item.qty + delta);
        renderCart();
    }

    function renderCart() {
        const cartDiv = document.getElementById('checkoutItems');
        if (!cartDiv) {
            return;
        }

        if (!state.cart.length) {
            cartDiv.innerHTML = '<div class="empty-cart">No items selected</div>';
            updateTotals();
            return;
        }

        cartDiv.innerHTML = '';

        state.cart.forEach(function (item) {
            const wrapper = document.createElement('div');
            wrapper.className = 'checkout-item';

            const details = document.createElement('div');
            details.className = 'checkout-item-details';

            const name = document.createElement('div');
            name.className = 'checkout-item-name';
            name.textContent = item.name;

            const qty = document.createElement('div');
            qty.className = 'checkout-item-qty';
            qty.textContent = 'Qty: ' + item.qty;

            details.appendChild(name);
            details.appendChild(qty);

            const actions = document.createElement('div');
            actions.className = 'checkout-item-actions';

            const minus = document.createElement('button');
            minus.type = 'button';
            minus.className = 'qty-btn';
            minus.textContent = '-';
            minus.addEventListener('click', function () {
                updateQty(item.id, -1);
            });

            const plus = document.createElement('button');
            plus.type = 'button';
            plus.className = 'qty-btn';
            plus.textContent = '+';
            plus.addEventListener('click', function () {
                updateQty(item.id, 1);
            });

            actions.appendChild(minus);
            actions.appendChild(plus);

            const price = document.createElement('div');
            price.className = 'checkout-item-price';
            price.textContent = formatCurrency(item.price * item.qty);

            const removeButton = document.createElement('button');
            removeButton.type = 'button';
            removeButton.className = 'checkout-item-remove';
            removeButton.textContent = 'x';
            removeButton.addEventListener('click', function () {
                removeFromCart(item.id);
            });

            wrapper.appendChild(details);
            wrapper.appendChild(actions);
            wrapper.appendChild(price);
            wrapper.appendChild(removeButton);
            cartDiv.appendChild(wrapper);
        });

        updateTotals();
    }

    function updateTotals() {
        const subtotal = state.cart.reduce(function (sum, item) {
            return sum + (item.price * item.qty);
        }, 0);
        const total = subtotal;

        setText('subtotal', subtotal.toFixed(2));
        setText('total', total.toFixed(2));
        setText('btnTotal', total.toFixed(2));
    }

    function clearCart() {
        if (!state.cart.length) {
            return;
        }

        if (window.confirm('Clear cart?')) {
            state.cart = [];
            clearFeedback();
            renderCart();
        }
    }

    function holdOrder() {
        if (!state.cart.length) {
            window.alert('No items selected.');
            return;
        }

        showFeedback('Order is on hold. Payment is not yet completed.', 'success');
    }

    function completeCheckout() {
        if (state.isSubmitting) {
            return;
        }

        if (!state.cart.length) {
            showFeedback('Cart is empty.', 'error');
            return;
        }

        const customerInput = document.getElementById('customerName');
        const customerName = String(customerInput?.value || '').trim();

        if (!customerName) {
            showFeedback('Customer name is required before payment.', 'error');
            customerInput?.focus();
            return;
        }

        if (!/^[a-zA-Z\s]+$/.test(customerName)) {
            showFeedback('Customer name must contain only letters and spaces.', 'error');
            customerInput?.focus();
            return;
        }

        const paymentInput = document.querySelector('input[name="paymentMethod"]:checked');
        const paymentMethod = paymentInput ? paymentInput.value : 'Cash';
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

        if (!config.checkoutUrl) {
            showFeedback('Checkout endpoint is not configured.', 'error');
            return;
        }

        state.isSubmitting = true;
        toggleCheckoutButton(true);
        showFeedback('Processing payment...', 'success');

        fetch(config.checkoutUrl, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token,
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: JSON.stringify({
                customerName: customerName,
                paymentMethod: paymentMethod,
                items: state.cart.map(function (item) {
                    return {
                        productId: parseInt(item.id, 10),
                        quantity: item.qty
                    };
                })
            })
        })
            .then(function (response) {
                return response.text().then(function (rawText) {
                    let data = {};
                    if (rawText) {
                        try {
                            data = JSON.parse(rawText);
                        } catch {
                            data = {
                                success: false,
                                message: 'Server returned a non-JSON response.',
                                details: rawText
                            };
                        }
                    }

                    return {
                        ok: response.ok,
                        status: response.status,
                        statusText: response.statusText,
                        data: data || {}
                    };
                });
            })
            .then(function (result) {
                if (!result.ok || !result.data.success) {
                    throw new Error(buildErrorDetails(result));
                }

                state.lastInvoice = result.data.invoice || null;
                state.cart = [];
                renderCart();
                resetCheckoutForm();
                clearErrorDetails();
                showFeedback(result.data.message + ' Print invoice is now ready.', 'success');
                togglePrintButton(true);
                loadProducts();
            })
            .catch(function (error) {
                const finalMessage = error && error.message ? error.message : 'Unable to complete payment.';
                showFeedback('Payment failed. Please check details below.', 'error');
                showErrorDetails(finalMessage);
            })
            .finally(function () {
                state.isSubmitting = false;
                toggleCheckoutButton(false);
            });
    }

    function printInvoice() {
        if (!state.lastInvoice || !state.lastInvoice.items || !state.lastInvoice.items.length) {
            showFeedback('Print is only available after a successful payment.', 'error');
            return;
        }

        const invoice = state.lastInvoice;
        const rows = invoice.items.map(function (item) {
            return '<tr>' +
                '<td>' + escapeHtml(item.productName) + '</td>' +
                '<td>' + item.quantity + '</td>' +
                '<td>' + formatCurrency(item.unitPrice) + '</td>' +
                '<td>' + formatCurrency(item.totalAmount) + '</td>' +
                '</tr>';
        }).join('');

        const invoiceHtml =
            '<html><head><title>Invoice</title>' +
            '<style>' +
            'body{font-family:Arial,sans-serif;margin:20px;max-width:600px;}' +
            '.invoice-header{text-align:center;margin-bottom:30px;border-bottom:2px solid #10b981;padding-bottom:15px;}' +
            '.invoice-header h1{margin:0;color:#10b981;}' +
            '.detail-row{display:flex;justify-content:space-between;margin-bottom:8px;}' +
            'table{width:100%;border-collapse:collapse;margin:20px 0;}' +
            'th,td{padding:10px;text-align:left;border-bottom:1px solid #ddd;}' +
            'th{background-color:#f3f4f6;font-weight:bold;}' +
            '.totals{margin-top:20px;border-top:2px solid #10b981;padding-top:15px;}' +
            '.total-row{display:flex;justify-content:space-between;font-size:16px;font-weight:bold;margin-bottom:10px;}' +
            '.final-total{font-size:20px;color:#10b981;}' +
            '.footer{text-align:center;margin-top:30px;color:#666;font-size:12px;}' +
            '</style></head><body>' +
            '<div class="invoice-header"><h1>POS INVOICE</h1></div>' +
            '<div class="detail-row"><span>Customer:</span><span>' + escapeHtml(invoice.customerName) + '</span></div>' +
            '<div class="detail-row"><span>Payment Method:</span><span>' + escapeHtml(invoice.paymentMethod) + '</span></div>' +
            '<div class="detail-row"><span>Date/Time:</span><span>' + escapeHtml(invoice.timestamp) + '</span></div>' +
            '<table><thead><tr><th>Item</th><th>Qty</th><th>Price</th><th>Total</th></tr></thead><tbody>' +
            rows +
            '</tbody></table>' +
            '<div class="totals">' +
            '<div class="total-row"><span>Total Amount:</span><span>' + formatCurrency(invoice.subtotal) + '</span></div>' +
            '<div class="total-row final-total"><span>TOTAL:</span><span>' + formatCurrency(invoice.total) + '</span></div>' +
            '</div>' +
            '<div class="footer"><p>Thank you for your purchase.</p><p>Printed: ' + escapeHtml(new Date().toLocaleString()) + '</p></div>' +
            '</body></html>';

        const printWindow = window.open('', '', 'width=800,height=600');
        if (!printWindow) {
            showFeedback('Unable to open print window.', 'error');
            return;
        }

        printWindow.document.open();
        printWindow.document.write(invoiceHtml);
        printWindow.document.close();
        window.setTimeout(function () {
            printWindow.print();
        }, 250);
    }

    function resetCheckoutForm() {
        const customerInput = document.getElementById('customerName');
        if (customerInput) {
            customerInput.value = '';
        }

        const cashOption = document.getElementById('paymentCash');
        if (cashOption) {
            cashOption.checked = true;
        }
    }

    function showFeedback(message, type) {
        const feedback = document.getElementById('paymentFeedback');
        if (!feedback) {
            return;
        }

        feedback.className = 'payment-feedback ' + (type === 'error' ? 'is-error' : 'is-success');
        feedback.textContent = message;
    }

    function clearFeedback() {
        const feedback = document.getElementById('paymentFeedback');
        if (!feedback) {
            return;
        }

        feedback.className = 'payment-feedback';
        feedback.textContent = '';
        clearErrorDetails();
    }

    function showErrorDetails(detailsText) {
        const details = document.getElementById('paymentErrorDetails');
        if (!details) {
            return;
        }

        details.textContent = detailsText || 'Unknown payment error.';
        details.className = 'payment-error-details is-visible';
    }

    function clearErrorDetails() {
        const details = document.getElementById('paymentErrorDetails');
        if (!details) {
            return;
        }

        details.textContent = '';
        details.className = 'payment-error-details';
    }

    function buildErrorDetails(result) {
        const payload = result && result.data ? result.data : {};
        const lines = [];
        lines.push('HTTP: ' + String(result.status || 'N/A') + ' ' + String(result.statusText || ''));

        if (payload.message) {
            lines.push('Message: ' + String(payload.message));
        }

        if (payload.details) {
            lines.push('Details: ' + String(payload.details));
        }

        if (!payload.message && !payload.details) {
            lines.push('Message: Payment request failed.');
        }

        return lines.join('\n');
    }

    function togglePrintButton(isVisible) {
        const button = document.getElementById('printInvoiceBtn');
        if (button) {
            button.hidden = !isVisible;
        }
    }

    function toggleCheckoutButton(isBusy) {
        const button = document.querySelector('.btn-checkout');
        if (!button) {
            return;
        }

        button.disabled = isBusy;
        button.style.opacity = isBusy ? '0.7' : '1';
        button.style.cursor = isBusy ? 'wait' : 'pointer';
    }

    function updateTime() {
        setText('currentTime', new Date().toLocaleString('en-US', {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit'
        }));
    }

    function setText(id, value) {
        const element = document.getElementById(id);
        if (element) {
            element.textContent = value;
        }
    }

    function formatCurrency(amount) {
        return 'PHP ' + Number(amount || 0).toFixed(2);
    }

    function escapeHtml(text) {
        return String(text == null ? '' : text)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    window.addToCart = addToCart;
    window.removeFromCart = removeFromCart;
    window.updateQty = updateQty;
    window.clearCart = clearCart;
    window.holdOrder = holdOrder;
    window.completeCheckout = completeCheckout;
    window.printInvoice = printInvoice;
    window.filterByCategory = filterByCategory;
})();
