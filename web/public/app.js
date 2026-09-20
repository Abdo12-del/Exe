// Modern Arabic POS System - Client Logic

let state = {
  settings: {},
  categories: [],
  products: [],
  cart: [], // { product, quantity }
  activeCategory: 'all',
  currentTab: 'pos',
  sales: [],
  stats: null,
  charts: {
    timeline: null,
    topProducts: null
  }
};

// DOM References
const elements = {
  headerStoreName: document.getElementById('headerStoreName'),
  liveClock: document.getElementById('liveClock'),
  navTabs: document.querySelectorAll('.nav-tab'),
  tabPanes: document.querySelectorAll('.tab-pane'),

  // POS
  posSearchInput: document.getElementById('posSearchInput'),
  btnFocusSearch: document.getElementById('btnFocusSearch'),
  posCategoryPills: document.getElementById('posCategoryPills'),
  posProductsGrid: document.getElementById('posProductsGrid'),
  cartItemsList: document.getElementById('cartItemsList'),
  cartCount: document.getElementById('cartCount'),
  cartSubtotal: document.getElementById('cartSubtotal'),
  cartDiscount: document.getElementById('cartDiscount'),
  cartPaid: document.getElementById('cartPaid'),
  cartTotal: document.getElementById('cartTotal'),
  cartChange: document.getElementById('cartChange'),
  btnCheckout: document.getElementById('btnCheckout'),
  btnClearCart: document.getElementById('btnClearCart'),

  // Inventory
  inventoryTableBody: document.getElementById('inventoryTableBody'),
  invSearchInput: document.getElementById('invSearchInput'),
  invCategoryFilter: document.getElementById('invCategoryFilter'),
  btnFilterLowStock: document.getElementById('btnFilterLowStock'),
  btnOpenAddProduct: document.getElementById('btnOpenAddProduct'),

  // Product Modal
  productModal: document.getElementById('productModal'),
  productModalTitle: document.getElementById('productModalTitle'),
  productForm: document.getElementById('productForm'),
  btnCloseProductModal: document.getElementById('btnCloseProductModal'),
  btnCancelProductModal: document.getElementById('btnCancelProductModal'),
  prodId: document.getElementById('prodId'),
  prodName: document.getElementById('prodName'),
  prodBarcode: document.getElementById('prodBarcode'),
  prodCategory: document.getElementById('prodCategory'),
  prodBuyPrice: document.getElementById('prodBuyPrice'),
  prodSellPrice: document.getElementById('prodSellPrice'),
  prodStock: document.getElementById('prodStock'),
  prodMinStock: document.getElementById('prodMinStock'),

  // Sales History
  salesTableBody: document.getElementById('salesTableBody'),
  salesSearchInput: document.getElementById('salesSearchInput'),
  salesDateFilter: document.getElementById('salesDateFilter'),
  btnResetSalesFilter: document.getElementById('btnResetSalesFilter'),

  // Stats
  kpiTodaySales: document.getElementById('kpiTodaySales'),
  kpiTodayCount: document.getElementById('kpiTodayCount'),
  kpiTodayProfit: document.getElementById('kpiTodayProfit'),
  kpiTotalSales: document.getElementById('kpiTotalSales'),
  kpiTotalInvoices: document.getElementById('kpiTotalInvoices'),
  kpiStockAlerts: document.getElementById('kpiStockAlerts'),
  statTotalItems: document.getElementById('statTotalItems'),
  statBuyVal: document.getElementById('statBuyVal'),
  statSellVal: document.getElementById('statSellVal'),
  statPotentialProfit: document.getElementById('statPotentialProfit'),
  btnRefreshStats: document.getElementById('btnRefreshStats'),

  // Settings
  settingsForm: document.getElementById('settingsForm'),
  setStoreName: document.getElementById('setStoreName'),
  setPhone: document.getElementById('setPhone'),
  setCurrency: document.getElementById('setCurrency'),
  setAddress: document.getElementById('setAddress'),
  setReceiptFooter: document.getElementById('setReceiptFooter'),
  importFileInput: document.getElementById('importFileInput'),
  btnResetDemoData: document.getElementById('btnResetDemoData'),

  // Receipt Modal
  receiptModal: document.getElementById('receiptModal'),
  btnCloseReceiptModal: document.getElementById('btnCloseReceiptModal'),
  btnDoneReceipt: document.getElementById('btnDoneReceipt'),
  btnPrintReceipt: document.getElementById('btnPrintReceipt'),
  recStoreName: document.getElementById('recStoreName'),
  recStoreAddress: document.getElementById('recStoreAddress'),
  recStorePhone: document.getElementById('recStorePhone'),
  recInvoiceNum: document.getElementById('recInvoiceNum'),
  recDate: document.getElementById('recDate'),
  recItemsBody: document.getElementById('recItemsBody'),
  recSubtotal: document.getElementById('recSubtotal'),
  recDiscount: document.getElementById('recDiscount'),
  recDiscountRow: document.getElementById('recDiscountRow'),
  recTotal: document.getElementById('recTotal'),
  recPaid: document.getElementById('recPaid'),
  recChange: document.getElementById('recChange'),
  recBarcodeCode: document.getElementById('recBarcodeCode'),
  recFooterMsg: document.getElementById('recFooterMsg'),

  toastContainer: document.getElementById('toastContainer')
};

// Utilities
function formatMoney(amount) {
  const currency = state.settings.currency || 'دج';
  const val = Number(amount) || 0;
  return `${val.toLocaleString('ar-DZ', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} ${currency}`;
}

function showToast(message, isError = false) {
  const toast = document.createElement('div');
  toast.className = `toast ${isError ? 'toast-error' : ''}`;
  toast.innerHTML = `
    <span style="font-size: 1.2rem;">${isError ? '⚠️' : '✅'}</span>
    <span>${message}</span>
  `;
  elements.toastContainer.appendChild(toast);
  setTimeout(() => {
    toast.style.opacity = '0';
    toast.style.transform = 'translateX(-100%)';
    toast.style.transition = 'all 0.3s ease';
    setTimeout(() => toast.remove(), 300);
  }, 3500);
}

// Live Clock
function updateClock() {
  const now = new Date();
  elements.liveClock.textContent = now.toLocaleDateString('ar-DZ', {
    weekday: 'long',
    year: 'numeric',
    month: 'numeric',
    day: 'numeric'
  }) + ' | ' + now.toLocaleTimeString('ar-DZ');
}
setInterval(updateClock, 1000);
updateClock();

// Tab Navigation
elements.navTabs.forEach(tab => {
  tab.addEventListener('click', () => {
    const target = tab.dataset.tab;
    switchTab(target);
  });
});

function switchTab(tabId) {
  state.currentTab = tabId;
  elements.navTabs.forEach(t => t.classList.toggle('active', t.dataset.tab === tabId));
  elements.tabPanes.forEach(p => p.classList.toggle('active', p.id === `pane-${tabId}`));

  if (tabId === 'pos') {
    elements.posSearchInput.focus();
    renderPosCatalog();
  } else if (tabId === 'products') {
    loadProducts();
  } else if (tabId === 'sales') {
    loadSales();
  } else if (tabId === 'stats') {
    loadStats();
  } else if (tabId === 'settings') {
    populateSettingsForm();
  }
}

// API Calls
async function fetchAPI(url, options = {}) {
  try {
    const res = await fetch(url, {
      headers: { 'Content-Type': 'application/json' },
      ...options
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({ error: 'فشل الطلب' }));
      throw new Error(err.error || `خطأ ${res.status}`);
    }
    return await res.json();
  } catch (err) {
    showToast(err.message, true);
    throw err;
  }
}

// Initial Data Load
async function initApp() {
  try {
    const [settings, categories] = await Promise.all([
      fetchAPI('/api/settings'),
      fetchAPI('/api/categories')
    ]);
    state.settings = settings;
    state.categories = categories;

    elements.headerStoreName.textContent = settings.storeName || 'متجر النور للمبيعات';
    renderCategoryPills();
    populateCategorySelects();
    await loadProducts();
  } catch (err) {
    console.error("Initialization error:", err);
  }
}

// Render Category Pills (POS)
function renderCategoryPills() {
  elements.posCategoryPills.innerHTML = state.categories.map(cat => `
    <button class="category-pill ${state.activeCategory === cat.id ? 'active' : ''}" data-cat="${cat.id}">
      ${cat.name}
    </button>
  `).join('');

  elements.posCategoryPills.querySelectorAll('.category-pill').forEach(btn => {
    btn.addEventListener('click', () => {
      state.activeCategory = btn.dataset.cat;
      renderCategoryPills();
      renderPosCatalog();
    });
  });
}

function populateCategorySelects() {
  const cats = state.categories.filter(c => c.id !== 'all');
  elements.prodCategory.innerHTML = cats.map(c => `<option value="${c.id}">${c.name}</option>`).join('');
  elements.invCategoryFilter.innerHTML = '<option value="all">كل التصنيفات</option>' +
    cats.map(c => `<option value="${c.id}">${c.name}</option>`).join('');
}

// Load Products
async function loadProducts() {
  try {
    const products = await fetchAPI('/api/products');
    state.products = products;
    renderPosCatalog();
    if (state.currentTab === 'products') {
      renderInventoryTable();
    }
  } catch (err) {
    console.error(err);
  }
}

// Render POS Catalog Grid
function renderPosCatalog() {
  const query = elements.posSearchInput.value.trim().toLowerCase();
  let list = state.products;

  if (state.activeCategory !== 'all') {
    list = list.filter(p => p.category === state.activeCategory);
  }
  if (query) {
    list = list.filter(p => 
      p.name.toLowerCase().includes(query) || 
      (p.barcode && p.barcode.toLowerCase().includes(query))
    );
  }

  if (list.length === 0) {
    elements.posProductsGrid.innerHTML = `
      <div style="grid-column: 1 / -1; text-align: center; padding: 3rem 1rem; color: var(--text-light);">
        <p style="font-size: 1.1rem; margin-bottom: 0.5rem;">لا توجد منتجات مطابقة للبحث</p>
        <span style="font-size: 0.85rem;">جرّب كلمة بحث أخرى أو تصنيفاً مختلفاً</span>
      </div>
    `;
    return;
  }

  elements.posProductsGrid.innerHTML = list.map(p => {
    const isLow = p.stock <= (p.minStock || 5) && p.stock > 0;
    const isOut = p.stock <= 0;
    return `
      <div class="product-card" data-id="${p.id}">
        ${isOut ? '<span class="product-badge-low" style="background: #fee2e2; color: #b91c1c;">نفد المخزون</span>' : ''}
        ${isLow ? '<span class="product-badge-low">مخزون قليل</span>' : ''}
        <div class="product-card-barcode">${p.barcode || '—'}</div>
        <div class="product-card-title">${p.name}</div>
        <div class="product-card-footer">
          <div class="product-card-price">${formatMoney(p.sellPrice)}</div>
          <div class="product-card-stock">المتاح: ${p.stock}</div>
        </div>
      </div>
    `;
  }).join('');

  elements.posProductsGrid.querySelectorAll('.product-card').forEach(card => {
    card.addEventListener('click', () => {
      const id = parseInt(card.dataset.id, 10);
      addToCart(id);
    });
  });
}

// Search input handling & barcode enter key
elements.posSearchInput.addEventListener('input', () => {
  renderPosCatalog();
});

elements.posSearchInput.addEventListener('keydown', (e) => {
  if (e.key === 'Enter') {
    e.preventDefault();
    const query = elements.posSearchInput.value.trim().toLowerCase();
    if (!query) return;

    // Exact barcode match first
    let found = state.products.find(p => p.barcode && p.barcode.toLowerCase() === query);
    if (!found) {
      // Partial name match
      found = state.products.find(p => p.name.toLowerCase().includes(query));
    }

    if (found) {
      addToCart(found.id);
      elements.posSearchInput.value = '';
      renderPosCatalog();
    } else {
      showToast("لم يتم العثور على أي منتج يطابق الباركود المدخل", true);
    }
  }
});

elements.btnFocusSearch.addEventListener('click', () => {
  elements.posSearchInput.focus();
  elements.posSearchInput.select();
});

// CART OPERATIONS
function addToCart(productId) {
  const product = state.products.find(p => p.id === productId);
  if (!product) return;

  if (product.stock <= 0) {
    showToast(`المنتج "${product.name}" نفد من المخزون!`, true);
    return;
  }

  const existing = state.cart.find(item => item.product.id === productId);
  if (existing) {
    if (existing.quantity + 1 > product.stock) {
      showToast(`لا يمكنك طلب أكثر من المخزون المتوفر (${product.stock}) للمنتج "${product.name}"`, true);
      return;
    }
    existing.quantity += 1;
  } else {
    state.cart.push({
      product,
      quantity: 1
    });
  }

  renderCart();
}

function updateCartQty(productId, delta) {
  const item = state.cart.find(it => it.product.id === productId);
  if (!item) return;

  const newQty = item.quantity + delta;
  if (newQty <= 0) {
    removeFromCart(productId);
    return;
  }
  if (newQty > item.product.stock) {
    showToast(`المخزون المتوفر هو ${item.product.stock} فقط`, true);
    return;
  }

  item.quantity = newQty;
  renderCart();
}

function removeFromCart(productId) {
  state.cart = state.cart.filter(it => it.product.id !== productId);
  renderCart();
}

elements.btnClearCart.addEventListener('click', () => {
  if (state.cart.length === 0) return;
  if (confirm("هل تريد تفريغ سلة المبيعات؟")) {
    state.cart = [];
    renderCart();
  }
});

function renderCart() {
  if (state.cart.length === 0) {
    elements.cartItemsList.innerHTML = `
      <div class="cart-empty-state">
        <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><circle cx="8" cy="21" r="1"/><circle cx="19" cy="21" r="1"/><path d="M2.05 2.05h2l2.66 12.42a2 2 0 0 0 2 1.58h9.78a2 2 0 0 0 1.95-1.57l1.65-7.43H5.12"/></svg>
        <p style="font-weight: 600;">السلة فارغة حالياً</p>
        <span style="font-size: 0.8rem;">اختر منتجات من القائمة أو امسح الباركود للبدء</span>
      </div>
    `;
    elements.cartCount.textContent = '0 مواد';
    elements.cartSubtotal.textContent = formatMoney(0);
    elements.cartTotal.textContent = formatMoney(0);
    elements.cartChange.textContent = formatMoney(0);
    elements.btnCheckout.disabled = true;
    return;
  }

  let subtotal = 0;
  let totalItemsCount = 0;

  elements.cartItemsList.innerHTML = state.cart.map(item => {
    const itemTotal = item.quantity * item.product.sellPrice;
    subtotal += itemTotal;
    totalItemsCount += item.quantity;

    return `
      <div class="cart-item-row">
        <div class="cart-item-info">
          <div class="cart-item-name">${item.product.name}</div>
          <div class="cart-item-price">${formatMoney(item.product.sellPrice)}</div>
        </div>
        <div class="cart-item-controls">
          <button class="btn-qty btn-minus" data-id="${item.product.id}">-</button>
          <span class="cart-item-qty">${item.quantity}</span>
          <button class="btn-qty btn-plus" data-id="${item.product.id}">+</button>
          <div class="cart-item-total">${formatMoney(itemTotal)}</div>
          <button class="btn-item-del" data-id="${item.product.id}" title="حذف">&times;</button>
        </div>
      </div>
    `;
  }).join('');

  // Attach controls
  elements.cartItemsList.querySelectorAll('.btn-minus').forEach(b => {
    b.addEventListener('click', () => updateCartQty(parseInt(b.dataset.id, 10), -1));
  });
  elements.cartItemsList.querySelectorAll('.btn-plus').forEach(b => {
    b.addEventListener('click', () => updateCartQty(parseInt(b.dataset.id, 10), 1));
  });
  elements.cartItemsList.querySelectorAll('.btn-item-del').forEach(b => {
    b.addEventListener('click', () => removeFromCart(parseInt(b.dataset.id, 10)));
  });

  elements.cartCount.textContent = `${totalItemsCount} مواد`;
  elements.cartSubtotal.textContent = formatMoney(subtotal);

  calculateCheckoutTotals(subtotal);
  elements.btnCheckout.disabled = false;
}

function calculateCheckoutTotals(subtotal) {
  const discount = Math.max(0, parseFloat(elements.cartDiscount.value) || 0);
  const total = Math.max(0, subtotal - discount);
  elements.cartTotal.textContent = formatMoney(total);

  const paid = parseFloat(elements.cartPaid.value) || 0;
  const change = paid > total ? paid - total : 0;
  elements.cartChange.textContent = formatMoney(change);

  return { subtotal, discount, total, paid, change };
}

elements.cartDiscount.addEventListener('input', () => {
  const subtotal = state.cart.reduce((acc, it) => acc + (it.quantity * it.product.sellPrice), 0);
  calculateCheckoutTotals(subtotal);
});

elements.cartPaid.addEventListener('input', () => {
  const subtotal = state.cart.reduce((acc, it) => acc + (it.quantity * it.product.sellPrice), 0);
  calculateCheckoutTotals(subtotal);
});

// CHECKOUT OPERATION
elements.btnCheckout.addEventListener('click', async () => {
  if (state.cart.length === 0) return;

  const subtotal = state.cart.reduce((acc, it) => acc + (it.quantity * it.product.sellPrice), 0);
  const calc = calculateCheckoutTotals(subtotal);

  const payload = {
    items: state.cart.map(it => ({
      productId: it.product.id,
      quantity: it.quantity
    })),
    discount: calc.discount,
    paidAmount: calc.paid > 0 ? calc.paid : calc.total,
    paymentMethod: 'cash'
  };

  try {
    elements.btnCheckout.disabled = true;
    elements.btnCheckout.innerHTML = 'جاري تسجيل البيع...';

    const res = await fetchAPI('/api/sales', {
      method: 'POST',
      body: JSON.stringify(payload)
    });

    showToast("✔ تمت عملية البيع بنجاح وتحديث المخزون");

    // Clear cart
    state.cart = [];
    elements.cartDiscount.value = 0;
    elements.cartPaid.value = '';
    renderCart();

    // Reload products to update stock quantities
    await loadProducts();

    // Open Thermal Receipt Modal
    showReceiptModal(res.sale);
  } catch (err) {
    console.error(err);
  } finally {
    elements.btnCheckout.disabled = false;
    elements.btnCheckout.innerHTML = `
      <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polyline points="20 6 9 17 4 12"/></svg>
      إتمام البيع وطباعة الفاتورة
    `;
  }
});

// THERMAL RECEIPT DISPLAY
function showReceiptModal(sale) {
  elements.recStoreName.textContent = state.settings.storeName || 'متجر النور للمبيعات';
  elements.recStoreAddress.textContent = state.settings.address || '';
  elements.recStorePhone.textContent = state.settings.phone ? `هاتف: ${state.settings.phone}` : '';
  elements.recInvoiceNum.textContent = `فاتورة رقم: ${sale.invoiceNumber}`;
  elements.recDate.textContent = `التاريخ: ${sale.dateStr}`;

  elements.recItemsBody.innerHTML = sale.items.map(it => `
    <tr>
      <td>${it.name}</td>
      <td style="text-align: center;">${it.quantity}</td>
      <td style="text-align: left;">${it.sellPrice}</td>
      <td style="text-align: left;">${it.total}</td>
    </tr>
  `).join('');

  elements.recSubtotal.textContent = formatMoney(sale.subtotal);
  if (sale.discount > 0) {
    elements.recDiscountRow.style.display = 'flex';
    elements.recDiscount.textContent = formatMoney(sale.discount);
  } else {
    elements.recDiscountRow.style.display = 'none';
  }
  elements.recTotal.textContent = formatMoney(sale.total);
  elements.recPaid.textContent = formatMoney(sale.paidAmount);
  elements.recChange.textContent = formatMoney(sale.changeAmount);
  elements.recBarcodeCode.textContent = sale.invoiceNumber;
  elements.recFooterMsg.textContent = state.settings.receiptFooter || 'شكراً لزيارتكم! مرحباً بكم دائماً.';

  elements.receiptModal.classList.add('active');
}

elements.btnCloseReceiptModal.addEventListener('click', () => {
  elements.receiptModal.classList.remove('active');
});
elements.btnDoneReceipt.addEventListener('click', () => {
  elements.receiptModal.classList.remove('active');
});
elements.btnPrintReceipt.addEventListener('click', () => {
  window.print();
});

// INVENTORY MANAGEMENT TAB
function renderInventoryTable() {
  const query = elements.invSearchInput.value.trim().toLowerCase();
  const cat = elements.invCategoryFilter.value;
  let list = state.products;

  if (cat !== 'all') {
    list = list.filter(p => p.category === cat);
  }
  if (query) {
    list = list.filter(p => 
      p.name.toLowerCase().includes(query) || 
      (p.barcode && p.barcode.toLowerCase().includes(query))
    );
  }

  if (list.length === 0) {
    elements.inventoryTableBody.innerHTML = `
      <tr>
        <td colspan="9" style="text-align: center; padding: 2rem; color: var(--text-muted);">
          لا توجد منتجات مطابقة للبحث
        </td>
      </tr>
    `;
    return;
  }

  const categoryNames = {
    food: "مواد غذائية",
    drinks: "مشروبات وعصائر",
    dairy: "ألبان وأجبان",
    sweets: "حلويات وبسكويت",
    cleaning: "منظفات ومستلزمات"
  };

  elements.inventoryTableBody.innerHTML = list.map(p => {
    const profitMargin = p.sellPrice - p.buyPrice;
    const profitPercent = p.buyPrice > 0 ? Math.round((profitMargin / p.buyPrice) * 100) : 0;
    const isOut = p.stock <= 0;
    const isLow = p.stock <= (p.minStock || 5) && !isOut;

    return `
      <tr>
        <td style="font-family: monospace; font-size: 0.85rem; color: var(--text-muted);">${p.barcode || '—'}</td>
        <td style="font-weight: 600;">${p.name}</td>
        <td><span class="badge badge-info">${categoryNames[p.category] || p.category}</span></td>
        <td>${formatMoney(p.buyPrice)}</td>
        <td style="font-weight: 700; color: var(--primary);">${formatMoney(p.sellPrice)}</td>
        <td style="color: var(--secondary); font-size: 0.85rem;">+${profitMargin} (${profitPercent}%)</td>
        <td style="font-weight: 700; font-size: 1rem;">${p.stock}</td>
        <td>
          ${isOut ? '<span class="badge badge-danger">نفد المخزون</span>' : ''}
          ${isLow ? '<span class="badge badge-warning">مخزون منخفض</span>' : ''}
          ${!isOut && !isLow ? '<span class="badge badge-success">متوفر</span>' : ''}
        </td>
        <td>
          <div style="display: flex; gap: 0.5rem;">
            <button class="btn btn-secondary btn-edit-prod" data-id="${p.id}" style="padding: 0.3rem 0.6rem; font-size: 0.8rem;">تعديل</button>
            <button class="btn btn-danger btn-del-prod" data-id="${p.id}" style="padding: 0.3rem 0.6rem; font-size: 0.8rem;">حذف</button>
          </div>
        </td>
      </tr>
    `;
  }).join('');

  elements.inventoryTableBody.querySelectorAll('.btn-edit-prod').forEach(b => {
    b.addEventListener('click', () => openEditProductModal(parseInt(b.dataset.id, 10)));
  });
  elements.inventoryTableBody.querySelectorAll('.btn-del-prod').forEach(b => {
    b.addEventListener('click', () => deleteProduct(parseInt(b.dataset.id, 10)));
  });
}

elements.invSearchInput.addEventListener('input', renderInventoryTable);
elements.invCategoryFilter.addEventListener('change', renderInventoryTable);

let lowStockFilterActive = false;
elements.btnFilterLowStock.addEventListener('click', () => {
  lowStockFilterActive = !lowStockFilterActive;
  elements.btnFilterLowStock.classList.toggle('btn-primary', lowStockFilterActive);
  elements.btnFilterLowStock.classList.toggle('btn-secondary', !lowStockFilterActive);
  if (lowStockFilterActive) {
    state.products = state.products.filter(p => p.stock <= (p.minStock || 5));
  } else {
    loadProducts();
  }
  renderInventoryTable();
});

// Product Add/Edit Modal
elements.btnOpenAddProduct.addEventListener('click', () => {
  elements.productModalTitle.textContent = "إضافة منتج جديد";
  elements.prodId.value = "";
  elements.productForm.reset();
  elements.prodMinStock.value = "5";
  elements.productModal.classList.add('active');
  elements.prodName.focus();
});

function openEditProductModal(productId) {
  const p = state.products.find(x => x.id === productId);
  if (!p) return;

  elements.productModalTitle.textContent = "تعديل بيانات المنتج";
  elements.prodId.value = p.id;
  elements.prodName.value = p.name;
  elements.prodBarcode.value = p.barcode || "";
  elements.prodCategory.value = p.category || "food";
  elements.prodBuyPrice.value = p.buyPrice;
  elements.prodSellPrice.value = p.sellPrice;
  elements.prodStock.value = p.stock;
  elements.prodMinStock.value = p.minStock || 5;

  elements.productModal.classList.add('active');
  elements.prodName.focus();
}

function closeProductModal() {
  elements.productModal.classList.remove('active');
}
elements.btnCloseProductModal.addEventListener('click', closeProductModal);
elements.btnCancelProductModal.addEventListener('click', closeProductModal);

elements.productForm.addEventListener('submit', async (e) => {
  e.preventDefault();
  const id = elements.prodId.value;
  const payload = {
    name: elements.prodName.value.trim(),
    barcode: elements.prodBarcode.value.trim(),
    category: elements.prodCategory.value,
    buyPrice: Number(elements.prodBuyPrice.value),
    sellPrice: Number(elements.prodSellPrice.value),
    stock: Number(elements.prodStock.value),
    minStock: Number(elements.prodMinStock.value)
  };

  try {
    if (id) {
      await fetchAPI(`/api/products/${id}`, {
        method: 'PUT',
        body: JSON.stringify(payload)
      });
      showToast("تم تحديث المنتج بنجاح");
    } else {
      await fetchAPI('/api/products', {
        method: 'POST',
        body: JSON.stringify(payload)
      });
      showToast("تمت إضافة المنتج بنجاح");
    }
    closeProductModal();
    await loadProducts();
  } catch (err) {
    console.error(err);
  }
});

async function deleteProduct(productId) {
  const p = state.products.find(x => x.id === productId);
  if (!p) return;

  if (confirm(`هل أنت متأكد من حذف المنتج "${p.name}"؟`)) {
    try {
      await fetchAPI(`/api/products/${productId}`, { method: 'DELETE' });
      showToast("تم حذف المنتج");
      await loadProducts();
    } catch (err) {
      console.error(err);
    }
  }
}

// SALES HISTORY TAB
async function loadSales() {
  try {
    const query = elements.salesSearchInput.value.trim();
    const date = elements.salesDateFilter.value;
    let url = '/api/sales?';
    if (query) url += `q=${encodeURIComponent(query)}&`;
    if (date) url += `date=${encodeURIComponent(date)}&`;

    const sales = await fetchAPI(url);
    state.sales = sales;
    renderSalesTable();
  } catch (err) {
    console.error(err);
  }
}

function renderSalesTable() {
  if (state.sales.length === 0) {
    elements.salesTableBody.innerHTML = `
      <tr>
        <td colspan="9" style="text-align: center; padding: 2.5rem; color: var(--text-muted);">
          لا توجد فواتير مبيعات مسجلة
        </td>
      </tr>
    `;
    return;
  }

  elements.salesTableBody.innerHTML = state.sales.map(s => {
    const totalItems = s.items.reduce((acc, it) => acc + it.quantity, 0);
    return `
      <tr>
        <td style="font-family: monospace; font-weight: 700; color: var(--primary-dark);">${s.invoiceNumber}</td>
        <td style="font-size: 0.85rem; color: var(--text-muted);">${s.dateStr}</td>
        <td style="text-align: center;">${totalItems} مواد</td>
        <td>${formatMoney(s.subtotal)}</td>
        <td style="color: var(--accent);">${s.discount > 0 ? '-' + formatMoney(s.discount) : '0'}</td>
        <td style="font-weight: 700; color: var(--primary);">${formatMoney(s.total)}</td>
        <td style="color: var(--secondary); font-weight: 600;">+${formatMoney(s.profit)}</td>
        <td><span class="badge badge-info">نقداً (Cash)</span></td>
        <td>
          <button class="btn btn-secondary btn-view-sale" data-id="${s.id}" style="padding: 0.35rem 0.75rem; font-size: 0.8rem;">
            معاينة
          </button>
        </td>
      </tr>
    `;
  }).join('');

  elements.salesTableBody.querySelectorAll('.btn-view-sale').forEach(b => {
    b.addEventListener('click', () => {
      const id = parseInt(b.dataset.id, 10);
      const sale = state.sales.find(s => s.id === id);
      if (sale) showReceiptModal(sale);
    });
  });
}

elements.salesSearchInput.addEventListener('input', loadSales);
elements.salesDateFilter.addEventListener('change', loadSales);
elements.btnResetSalesFilter.addEventListener('click', () => {
  elements.salesSearchInput.value = '';
  elements.salesDateFilter.value = '';
  loadSales();
});

// STATS & DASHBOARD TAB
async function loadStats() {
  try {
    const stats = await fetchAPI('/api/stats');
    state.stats = stats;

    elements.kpiTodaySales.textContent = formatMoney(stats.today.total);
    elements.kpiTodayCount.textContent = `${stats.today.count} فواتير`;
    elements.kpiTodayProfit.textContent = formatMoney(stats.today.profit);
    elements.kpiTotalSales.textContent = formatMoney(stats.overall.totalSales);
    elements.kpiTotalInvoices.textContent = `${stats.overall.totalSalesCount} عملية بيع (متوسط: ${formatMoney(stats.overall.averageTicket)})`;
    elements.kpiStockAlerts.textContent = stats.inventory.lowStockCount + stats.inventory.outOfStockCount;

    elements.statTotalItems.textContent = stats.inventory.totalProducts;
    elements.statBuyVal.textContent = formatMoney(stats.inventory.buyValue);
    elements.statSellVal.textContent = formatMoney(stats.inventory.sellValue);
    elements.statPotentialProfit.textContent = formatMoney(stats.inventory.potentialProfit);

    renderCharts(stats);
  } catch (err) {
    console.error(err);
  }
}

elements.btnRefreshStats.addEventListener('click', loadStats);

function renderCharts(stats) {
  if (typeof Chart === 'undefined') return;

  // 1. Timeline Chart
  const ctxTimeline = document.getElementById('salesTimelineChart').getContext('2d');
  if (state.charts.timeline) state.charts.timeline.destroy();

  const labels = stats.last7Days.map(d => d.label);
  const salesData = stats.last7Days.map(d => d.total);
  const profitData = stats.last7Days.map(d => d.profit);

  state.charts.timeline = new Chart(ctxTimeline, {
    type: 'bar',
    data: {
      labels: labels,
      datasets: [
        {
          label: 'المبيعات (دج)',
          data: salesData,
          backgroundColor: '#059669',
          borderRadius: 6
        },
        {
          label: 'الأرباح (دج)',
          data: profitData,
          backgroundColor: '#0284c7',
          borderRadius: 6
        }
      ]
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: { position: 'top', rtl: true, labels: { font: { family: 'Segoe UI' } } }
      },
      scales: {
        y: { beginAtZero: true, grid: { color: '#f1f5f9' } },
        x: { grid: { display: false } }
      }
    }
  });

  // 2. Top Products Chart
  const ctxTop = document.getElementById('topProductsChart').getContext('2d');
  if (state.charts.topProducts) state.charts.topProducts.destroy();

  const topLabels = stats.topProducts.map(p => p.name.length > 15 ? p.name.slice(0, 15) + '...' : p.name);
  const topCounts = stats.topProducts.map(p => p.count);

  state.charts.topProducts = new Chart(ctxTop, {
    type: 'doughnut',
    data: {
      labels: topLabels.length ? topLabels : ['لا توجد مبيعات بعد'],
      datasets: [{
        data: topCounts.length ? topCounts : [1],
        backgroundColor: [
          '#059669', '#0284c7', '#f59e0b', '#8b5cf6', '#ec4899', '#14b8a6'
        ]
      }]
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: { position: 'bottom', rtl: true, labels: { boxWidth: 12, font: { family: 'Segoe UI', size: 11 } } }
      }
    }
  });
}

// SETTINGS & BACKUP TAB
function populateSettingsForm() {
  elements.setStoreName.value = state.settings.storeName || '';
  elements.setPhone.value = state.settings.phone || '';
  elements.setCurrency.value = state.settings.currency || 'دج';
  elements.setAddress.value = state.settings.address || '';
  elements.setReceiptFooter.value = state.settings.receiptFooter || '';
}

elements.settingsForm.addEventListener('submit', async (e) => {
  e.preventDefault();
  const payload = {
    storeName: elements.setStoreName.value.trim(),
    phone: elements.setPhone.value.trim(),
    currency: elements.setCurrency.value.trim() || 'دج',
    address: elements.setAddress.value.trim(),
    receiptFooter: elements.setReceiptFooter.value.trim()
  };

  try {
    const res = await fetchAPI('/api/settings', {
      method: 'POST',
      body: JSON.stringify(payload)
    });
    state.settings = res.settings;
    elements.headerStoreName.textContent = res.settings.storeName;
    showToast("✔ تم حفظ الإعدادات بنجاح");
  } catch (err) {
    console.error(err);
  }
});

// Import JSON
elements.importFileInput.addEventListener('change', async (e) => {
  const file = e.target.files[0];
  if (!file) return;

  const reader = new FileReader();
  reader.onload = async (evt) => {
    try {
      const data = JSON.parse(evt.target.result);
      await fetchAPI('/api/import', {
        method: 'POST',
        body: JSON.stringify(data)
      });
      showToast("✔ تم استيراد قاعدة البيانات بنجاح!");
      await initApp();
      switchTab('pos');
    } catch (err) {
      showToast("ملف غير صالح أو تالف", true);
    }
  };
  reader.readAsText(file);
});

// Reset Demo Data
elements.btnResetDemoData.addEventListener('click', async () => {
  if (confirm("هل تريد استعادة البيانات النموذجية الافتراضية؟ سيتم تحديث المنتجات والفواتير التجريبية.")) {
    try {
      await fetchAPI('/api/reset-demo', { method: 'POST' });
      showToast("✔ تمت استعادة البيانات النموذجية");
      await initApp();
      switchTab('pos');
    } catch (err) {
      console.error(err);
    }
  }
});

// Start application
initApp();
