const express = require('express');
const cors = require('cors');
const fs = require('fs');
const path = require('path');

const app = express();
const PORT = process.env.PORT || 3000;
const DATA_FILE = path.join(__dirname, 'data.json');

app.use(cors());
app.use(express.json({ limit: '10mb' }));
app.use(express.static(path.join(__dirname, 'public')));

// Initial Default Data
const DEFAULT_DATA = {
  settings: {
    storeName: "متجر النور للمبيعات والتوزيع",
    phone: "0550 12 34 56",
    address: "شارع الاستقلال، الخروب - قسنطينة",
    currency: "دج",
    taxRate: 0,
    receiptFooter: "شكراً لزيارتكم! البضاعة المباعة ترد أو تستبدل خلال 48 ساعة مع الفاتورة."
  },
  categories: [
    { id: "all", name: "كل المنتجات", icon: "grid" },
    { id: "food", name: "مواد غذائية", icon: "shopping-bag" },
    { id: "drinks", name: "مشروبات وعصائر", icon: "coffee" },
    { id: "dairy", name: "ألبان وأجبان", icon: "package" },
    { id: "sweets", name: "حلويات وبسكويت", icon: "award" },
    { id: "cleaning", name: "منظفات ومستلزمات", icon: "sparkles" }
  ],
  products: [
    { id: 1, barcode: "6130001001", name: "زيت زيتون بكر ممتاز 1 لتر", category: "food", buyPrice: 900, sellPrice: 1200, stock: 18, minStock: 5 },
    { id: 2, barcode: "6130001002", name: "عسل سدر طبيعي 500 غرام", category: "food", buyPrice: 1600, sellPrice: 2300, stock: 12, minStock: 4 },
    { id: 3, barcode: "6130001003", name: "تمر دقلة نور أصلي 1 كلغ", category: "food", buyPrice: 450, sellPrice: 700, stock: 35, minStock: 10 },
    { id: 4, barcode: "6130001004", name: "حليب معقم كامل الدسم 1 لتر", category: "dairy", buyPrice: 95, sellPrice: 125, stock: 4, minStock: 15 },
    { id: 5, barcode: "6130001005", name: "جبن طري تقليدي 24 قطعة", category: "dairy", buyPrice: 280, sellPrice: 370, stock: 22, minStock: 8 },
    { id: 6, barcode: "6130001006", name: "مشروب غازي حمود بوعلام 1 لتر", category: "drinks", buyPrice: 85, sellPrice: 110, stock: 48, minStock: 12 },
    { id: 7, barcode: "6130001007", name: "عصير برتقال طبيعي 1 لتر", category: "drinks", buyPrice: 140, sellPrice: 190, stock: 19, minStock: 6 },
    { id: 8, barcode: "6130001008", name: "ماء معدني طبيعي 1.5 لتر", category: "drinks", buyPrice: 35, sellPrice: 50, stock: 60, minStock: 20 },
    { id: 9, barcode: "6130001009", name: "شوكولاتة بالحليب والمكسرات", category: "sweets", buyPrice: 160, sellPrice: 240, stock: 3, minStock: 10 },
    { id: 10, barcode: "6130001010", name: "بسكويت شاي تقليدي علبة", category: "sweets", buyPrice: 70, sellPrice: 100, stock: 26, minStock: 8 },
    { id: 11, barcode: "6130001011", name: "سائل غسيل الأواني ليمون 750 مل", category: "cleaning", buyPrice: 180, sellPrice: 250, stock: 14, minStock: 5 },
    { id: 12, barcode: "6130001012", name: "مسحوق غسيل أوتوماتيك 3 كلغ", category: "cleaning", buyPrice: 650, sellPrice: 850, stock: 8, minStock: 4 }
  ],
  customers: [
    { id: 1, name: "زبون عابر (نقدي)", phone: "", address: "", debt: 0, creditLimit: 0, notes: "مبيعات عامة مباشرة" },
    { id: 2, name: "محمد بلقاسم", phone: "0661 22 33 44", address: "حي النصر، عمارة 4", debt: 2500, creditLimit: 20000, notes: "زبون وفي، تسديد أسبوعي" },
    { id: 3, name: "مطعم السعادة (كمال)", phone: "0770 55 66 77", address: "الشارع الرئيسي، وسط المدينة", debt: 7800, creditLimit: 50000, notes: "طلبيات جملة نصف شهرية" },
    { id: 4, name: "أمينة شريف", phone: "0555 88 99 00", address: "حي الزهور، فيلا 12", debt: 0, creditLimit: 10000, notes: "تسديد فوري دائماً" }
  ],
  customerPayments: [
    { id: 1, customerId: 2, amount: 2000, dateStr: "2026-09-19 16:45", note: "دفعة نقدية على الحساب" }
  ],
  sales: [
    {
      id: 1001,
      invoiceNumber: "INV-2026-0001",
      timestamp: "2026-09-20T08:30:00.000Z",
      dateStr: "2026-09-20 09:30",
      customerId: 1,
      customerName: "زبون عابر (نقدي)",
      items: [
        { productId: 1, name: "زيت زيتون بكر ممتاز 1 لتر", quantity: 2, sellPrice: 1200, buyPrice: 900, total: 2400 },
        { productId: 8, name: "ماء معدني طبيعي 1.5 لتر", quantity: 6, sellPrice: 50, buyPrice: 35, total: 300 }
      ],
      subtotal: 2700,
      tax: 0,
      discount: 0,
      total: 2700,
      paidAmount: 2700,
      changeAmount: 0,
      debtAmount: 0,
      profit: 690,
      paymentMethod: "cash"
    },
    {
      id: 1002,
      invoiceNumber: "INV-2026-0002",
      timestamp: "2026-09-20T10:15:00.000Z",
      dateStr: "2026-09-20 11:15",
      customerId: 2,
      customerName: "محمد بلقاسم",
      items: [
        { productId: 3, name: "تمر دقلة نور أصلي 1 كلغ", quantity: 3, sellPrice: 700, buyPrice: 450, total: 2100 },
        { productId: 2, name: "عسل سدر طبيعي 500 غرام", quantity: 1, sellPrice: 2300, buyPrice: 1600, total: 2300 }
      ],
      subtotal: 4400,
      tax: 0,
      discount: 100,
      total: 4300,
      paidAmount: 1800,
      changeAmount: 0,
      debtAmount: 2500,
      profit: 1350,
      paymentMethod: "partial"
    }
  ],
  nextProductId: 13,
  nextSaleId: 1003,
  nextCustomerId: 5,
  nextPaymentId: 2,
  register: {
    isOpen: true,
    openedAt: "2026-09-20 08:00",
    openingFloat: 10000 // رصيد الصندوق الافتتاحي
  }
};

function loadData() {
  try {
    if (fs.existsSync(DATA_FILE)) {
      const raw = fs.readFileSync(DATA_FILE, 'utf8');
      const parsed = JSON.parse(raw);
      // Ensure defaults for backwards compatibility
      if (!parsed.customers) parsed.customers = DEFAULT_DATA.customers;
      if (!parsed.customerPayments) parsed.customerPayments = DEFAULT_DATA.customerPayments;
      if (!parsed.nextCustomerId) parsed.nextCustomerId = 5;
      if (!parsed.nextPaymentId) parsed.nextPaymentId = 2;
      if (!parsed.register) parsed.register = DEFAULT_DATA.register;
      return parsed;
    }
  } catch (err) {
    console.error("Error reading data.json, using defaults:", err);
  }
  saveData(DEFAULT_DATA);
  return JSON.parse(JSON.stringify(DEFAULT_DATA));
}

function saveData(data) {
  try {
    fs.writeFileSync(DATA_FILE, JSON.stringify(data, null, 2), 'utf8');
    return true;
  } catch (err) {
    console.error("Error saving data.json:", err);
    return false;
  }
}

// ----------------- SETTINGS & CATEGORIES -----------------

app.get('/api/settings', (req, res) => {
  const data = loadData();
  res.json(data.settings);
});

app.post('/api/settings', (req, res) => {
  const data = loadData();
  data.settings = { ...data.settings, ...req.body };
  saveData(data);
  res.json({ success: true, settings: data.settings });
});

app.get('/api/categories', (req, res) => {
  const data = loadData();
  res.json(data.categories || []);
});

// ----------------- PRODUCTS & INVENTORY -----------------

app.get('/api/products', (req, res) => {
  const data = loadData();
  let list = data.products || [];
  const { q, category, lowStock } = req.query;

  if (category && category !== 'all') {
    list = list.filter(p => p.category === category);
  }
  if (q) {
    const query = q.trim().toLowerCase();
    list = list.filter(p => 
      p.name.toLowerCase().includes(query) || 
      (p.barcode && p.barcode.toLowerCase().includes(query))
    );
  }
  if (lowStock === 'true') {
    list = list.filter(p => p.stock <= (p.minStock || 5));
  }
  res.json(list);
});

app.post('/api/products', (req, res) => {
  const data = loadData();
  const { name, category, buyPrice, sellPrice, stock, minStock, barcode } = req.body;

  if (!name || sellPrice === undefined) {
    return res.status(400).json({ error: "اسم المنتج وسعر البيع مطلوبان" });
  }

  const newId = data.nextProductId++;
  const newProduct = {
    id: newId,
    barcode: barcode || ("613" + String(newId).padStart(7, '0')),
    name: name.trim(),
    category: category || "food",
    buyPrice: Number(buyPrice) || 0,
    sellPrice: Number(sellPrice) || 0,
    stock: Number(stock) || 0,
    minStock: Number(minStock) || 5
  };

  data.products.push(newProduct);
  saveData(data);
  res.status(201).json(newProduct);
});

app.put('/api/products/:id', (req, res) => {
  const data = loadData();
  const id = parseInt(req.params.id, 10);
  const idx = data.products.findIndex(p => p.id === id);

  if (idx === -1) {
    return res.status(404).json({ error: "المنتج غير موجود" });
  }

  const existing = data.products[idx];
  const { name, category, buyPrice, sellPrice, stock, minStock, barcode } = req.body;

  data.products[idx] = {
    ...existing,
    name: name !== undefined ? name.trim() : existing.name,
    category: category !== undefined ? category : existing.category,
    barcode: barcode !== undefined ? barcode.trim() : existing.barcode,
    buyPrice: buyPrice !== undefined ? Number(buyPrice) : existing.buyPrice,
    sellPrice: sellPrice !== undefined ? Number(sellPrice) : existing.sellPrice,
    stock: stock !== undefined ? Number(stock) : existing.stock,
    minStock: minStock !== undefined ? Number(minStock) : existing.minStock
  };

  saveData(data);
  res.json(data.products[idx]);
});

app.delete('/api/products/:id', (req, res) => {
  const data = loadData();
  const id = parseInt(req.params.id, 10);
  const initialLen = data.products.length;
  data.products = data.products.filter(p => p.id !== id);

  if (data.products.length === initialLen) {
    return res.status(404).json({ error: "المنتج غير موجود" });
  }

  saveData(data);
  res.json({ success: true, message: "تم حذف المنتج بنجاح" });
});

// ----------------- CUSTOMERS & DEBTS (الزبائن والكريدي) -----------------

app.get('/api/customers', (req, res) => {
  const data = loadData();
  let list = data.customers || [];
  const { q, withDebt } = req.query;

  if (q) {
    const query = q.trim().toLowerCase();
    list = list.filter(c => 
      c.name.toLowerCase().includes(query) || 
      (c.phone && c.phone.includes(query))
    );
  }
  if (withDebt === 'true') {
    list = list.filter(c => (c.debt || 0) > 0);
  }

  res.json(list);
});

app.post('/api/customers', (req, res) => {
  const data = loadData();
  const { name, phone, address, creditLimit = 10000, notes = '' } = req.body;

  if (!name) {
    return res.status(400).json({ error: "اسم الزبون مطلوب" });
  }

  const newId = data.nextCustomerId++;
  const customer = {
    id: newId,
    name: name.trim(),
    phone: phone ? phone.trim() : '',
    address: address ? address.trim() : '',
    debt: 0,
    creditLimit: Number(creditLimit) || 10000,
    notes: notes ? notes.trim() : '',
    createdAt: new Date().toISOString()
  };

  data.customers.push(customer);
  saveData(data);
  res.status(201).json(customer);
});

app.put('/api/customers/:id', (req, res) => {
  const data = loadData();
  const id = parseInt(req.params.id, 10);
  const idx = data.customers.findIndex(c => c.id === id);
  if (idx === -1) return res.status(404).json({ error: "الزبون غير موجود" });

  const existing = data.customers[idx];
  const { name, phone, address, creditLimit, notes } = req.body;

  data.customers[idx] = {
    ...existing,
    name: name !== undefined ? name.trim() : existing.name,
    phone: phone !== undefined ? phone.trim() : existing.phone,
    address: address !== undefined ? address.trim() : existing.address,
    creditLimit: creditLimit !== undefined ? Number(creditLimit) : existing.creditLimit,
    notes: notes !== undefined ? notes.trim() : existing.notes
  };

  saveData(data);
  res.json(data.customers[idx]);
});

// Record customer debt payment (تسديد دين)
app.post('/api/customers/:id/pay', (req, res) => {
  const data = loadData();
  const id = parseInt(req.params.id, 10);
  const customer = data.customers.find(c => c.id === id);
  if (!customer) return res.status(404).json({ error: "الزبون غير موجود" });

  const amount = Number(req.body.amount) || 0;
  if (amount <= 0) {
    return res.status(400).json({ error: "مبلغ التسديد يجب أن يكون أكبر من الصفر" });
  }

  const previousDebt = customer.debt || 0;
  customer.debt = Math.max(0, previousDebt - amount);

  const now = new Date();
  const pad = n => String(n).padStart(2, '0');
  const dateStr = `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())} ${pad(now.getHours())}:${pad(now.getMinutes())}`;

  const payment = {
    id: data.nextPaymentId++,
    customerId: customer.id,
    customerName: customer.name,
    amount: amount,
    previousDebt,
    remainingDebt: customer.debt,
    dateStr,
    note: req.body.note ? req.body.note.trim() : 'تسديد نقدي'
  };

  data.customerPayments.unshift(payment);
  saveData(data);

  res.json({
    success: true,
    message: `تم تسديد مبلغ ${amount} دج للزبون "${customer.name}" بنجاح`,
    payment,
    customer
  });
});

// Customer history (sales + payments)
app.get('/api/customers/:id/history', (req, res) => {
  const data = loadData();
  const id = parseInt(req.params.id, 10);
  const customer = data.customers.find(c => c.id === id);
  if (!customer) return res.status(404).json({ error: "الزبون غير موجود" });

  const sales = (data.sales || []).filter(s => s.customerId === id);
  const payments = (data.customerPayments || []).filter(p => p.customerId === id);

  res.json({
    customer,
    sales,
    payments
  });
});

// ----------------- CHECKOUT & SALES -----------------

app.post('/api/sales', (req, res) => {
  const data = loadData();
  const { 
    items, 
    discount = 0, 
    paidAmount = 0, 
    paymentMethod = 'cash', 
    customerId = 1 
  } = req.body;

  if (!items || !Array.isArray(items) || items.length === 0) {
    return res.status(400).json({ error: "سلة المشتريات فارغة" });
  }

  // Find customer
  const customer = data.customers.find(c => c.id === parseInt(customerId, 10)) || data.customers[0];

  // Validate stock and prepare sale items
  let subtotal = 0;
  let totalCost = 0;
  const processedItems = [];

  for (const item of items) {
    const product = data.products.find(p => p.id === item.productId);
    if (!product) {
      return res.status(400).json({ error: `المنتج رقم ${item.productId} غير موجود` });
    }
    const qty = parseInt(item.quantity, 10);
    if (qty <= 0) {
      return res.status(400).json({ error: `كمية غير صالحة للمنتج: ${product.name}` });
    }
    if (product.stock < qty) {
      return res.status(400).json({ 
        error: `المخزون غير كافٍ للمنتج "${product.name}". المتاح حالياً: ${product.stock} فقط.` 
      });
    }

    const itemTotal = qty * product.sellPrice;
    const itemCost = qty * product.buyPrice;
    subtotal += itemTotal;
    totalCost += itemCost;

    processedItems.push({
      productId: product.id,
      name: product.name,
      barcode: product.barcode,
      category: product.category,
      quantity: qty,
      sellPrice: product.sellPrice,
      buyPrice: product.buyPrice,
      total: itemTotal
    });
  }

  // Deduct stock
  for (const item of items) {
    const product = data.products.find(p => p.id === item.productId);
    product.stock -= parseInt(item.quantity, 10);
  }

  const finalDiscount = Number(discount) || 0;
  const total = Math.max(0, subtotal - finalDiscount);
  const profit = Math.max(0, (subtotal - totalCost) - finalDiscount);

  let paid = 0;
  let debt = 0;
  let change = 0;

  if (paymentMethod === 'credit') {
    // بالكامل بالدين
    paid = 0;
    debt = total;
  } else if (paymentMethod === 'partial') {
    paid = Math.min(total, Number(paidAmount) || 0);
    debt = Math.max(0, total - paid);
  } else {
    // نقداً Cash
    const rawPaid = Number(paidAmount) || total;
    paid = rawPaid >= total ? total : rawPaid;
    change = rawPaid > total ? (rawPaid - total) : 0;
    debt = rawPaid < total ? (total - rawPaid) : 0;
  }

  // Update customer debt if any
  if (debt > 0) {
    customer.debt = (customer.debt || 0) + debt;
  }

  const saleId = data.nextSaleId++;
  const now = new Date();
  const pad = n => String(n).padStart(2, '0');
  const dateStr = `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())} ${pad(now.getHours())}:${pad(now.getMinutes())}`;

  const sale = {
    id: saleId,
    invoiceNumber: `INV-${now.getFullYear()}-${String(saleId).padStart(4, '0')}`,
    timestamp: now.toISOString(),
    dateStr: dateStr,
    customerId: customer.id,
    customerName: customer.name,
    items: processedItems,
    subtotal,
    discount: finalDiscount,
    total,
    paidAmount: paid,
    changeAmount: change,
    debtAmount: debt,
    customerRemainingTotalDebt: customer.debt || 0,
    profit,
    paymentMethod
  };

  data.sales.unshift(sale);
  saveData(data);

  res.status(201).json({
    success: true,
    sale,
    customer,
    message: "تم تسجيل عملية البيع بنجاح وتحديث المخزون"
  });
});

app.get('/api/sales', (req, res) => {
  const data = loadData();
  const { date, q, customerId } = req.query;
  let list = data.sales || [];

  if (date) {
    list = list.filter(s => s.dateStr && s.dateStr.startsWith(date));
  }
  if (customerId) {
    list = list.filter(s => s.customerId === parseInt(customerId, 10));
  }
  if (q) {
    const query = q.trim().toLowerCase();
    list = list.filter(s => 
      s.invoiceNumber.toLowerCase().includes(query) ||
      (s.customerName && s.customerName.toLowerCase().includes(query)) ||
      s.items.some(i => i.name.toLowerCase().includes(query))
    );
  }

  res.json(list);
});

// ----------------- CASH DRAWER & CLOSING (يومية الصندوق) -----------------

app.get('/api/register/today', (req, res) => {
  const data = loadData();
  const today = new Date().toISOString().slice(0, 10);

  const todaySales = (data.sales || []).filter(s => s.dateStr && s.dateStr.startsWith(today));
  const todayPayments = (data.customerPayments || []).filter(p => p.dateStr && p.dateStr.startsWith(today));

  let cashSalesTotal = 0;
  let creditSalesTotal = 0;
  let totalSalesAmount = 0;
  let todayProfit = 0;

  todaySales.forEach(s => {
    totalSalesAmount += s.total || 0;
    cashSalesTotal += s.paidAmount || 0;
    creditSalesTotal += s.debtAmount || 0;
    todayProfit += s.profit || 0;
  });

  const debtCollections = todayPayments.reduce((acc, p) => acc + (p.amount || 0), 0);
  const openingFloat = data.register?.openingFloat || 10000;
  const expectedCashInDrawer = openingFloat + cashSalesTotal + debtCollections;

  res.json({
    date: today,
    openingFloat,
    totalSalesAmount,
    cashSalesTotal,
    creditSalesTotal,
    debtCollections,
    expectedCashInDrawer,
    todayProfit,
    invoicesCount: todaySales.length,
    paymentsCount: todayPayments.length
  });
});

// ----------------- STATS & ANALYTICS -----------------

app.get('/api/stats', (req, res) => {
  const data = loadData();
  const products = data.products || [];
  const sales = data.sales || [];
  const customers = data.customers || [];

  const today = new Date().toISOString().slice(0, 10);
  let todaySalesTotal = 0;
  let todaySalesCount = 0;
  let todayProfit = 0;

  let totalSalesAmount = 0;
  let totalProfit = 0;

  const productSalesCount = {};

  sales.forEach(s => {
    totalSalesAmount += s.total || 0;
    totalProfit += s.profit || 0;

    if (s.dateStr && s.dateStr.startsWith(today)) {
      todaySalesTotal += s.total || 0;
      todaySalesCount += 1;
      todayProfit += s.profit || 0;
    }

    if (s.items && Array.isArray(s.items)) {
      s.items.forEach(it => {
        productSalesCount[it.name] = (productSalesCount[it.name] || 0) + (it.quantity || 1);
      });
    }
  });

  const topProducts = Object.entries(productSalesCount)
    .map(([name, count]) => ({ name, count }))
    .sort((a, b) => b.count - a.count)
    .slice(0, 6);

  let inventoryBuyValue = 0;
  let inventorySellValue = 0;
  let lowStockCount = 0;
  let outOfStockCount = 0;

  products.forEach(p => {
    inventoryBuyValue += (p.buyPrice || 0) * (p.stock || 0);
    inventorySellValue += (p.sellPrice || 0) * (p.stock || 0);
    if (p.stock <= 0) {
      outOfStockCount++;
    } else if (p.stock <= (p.minStock || 5)) {
      lowStockCount++;
    }
  });

  const totalCustomerDebts = customers.reduce((acc, c) => acc + (c.debt || 0), 0);

  const last7Days = [];
  for (let i = 6; i >= 0; i--) {
    const d = new Date();
    d.setDate(d.getDate() - i);
    const dStr = d.toISOString().slice(0, 10);
    const daySales = sales.filter(s => s.dateStr && s.dateStr.startsWith(dStr));
    const dayTotal = daySales.reduce((acc, cur) => acc + (cur.total || 0), 0);
    const dayProfit = daySales.reduce((acc, cur) => acc + (cur.profit || 0), 0);
    last7Days.push({
      date: dStr,
      label: `${d.getDate()}/${d.getMonth()+1}`,
      total: dayTotal,
      profit: dayProfit,
      count: daySales.length
    });
  }

  res.json({
    today: {
      total: todaySalesTotal,
      count: todaySalesCount,
      profit: todayProfit
    },
    overall: {
      totalSales: totalSalesAmount,
      totalSalesCount: sales.length,
      totalProfit: totalProfit,
      averageTicket: sales.length ? Math.round(totalSalesAmount / sales.length) : 0,
      totalCustomerDebts
    },
    inventory: {
      totalProducts: products.length,
      buyValue: inventoryBuyValue,
      sellValue: inventorySellValue,
      potentialProfit: inventorySellValue - inventoryBuyValue,
      lowStockCount,
      outOfStockCount
    },
    topProducts,
    last7Days
  });
});

// ----------------- CSV EXPORTS (Excel Compatible) -----------------

app.get('/api/export/sales-csv', (req, res) => {
  const data = loadData();
  const sales = data.sales || [];

  let csv = '\uFEFF'; // UTF-8 BOM so Excel opens Arabic correctly
  csv += 'رقم الفاتورة,التاريخ والوقت,اسم الزبون,عدد المواد,المجموع الفرعي,التخفيض,الإجمالي,المدفوع,الدين,الربح الصافي,طريقة الدفع\n';

  sales.forEach(s => {
    const itemCount = s.items ? s.items.reduce((acc, it) => acc + it.quantity, 0) : 0;
    csv += `"${s.invoiceNumber}","${s.dateStr}","${s.customerName || ''}",${itemCount},${s.subtotal},${s.discount},${s.total},${s.paidAmount},${s.debtAmount || 0},${s.profit},"${s.paymentMethod}"\n`;
  });

  res.setHeader('Content-Type', 'text/csv; charset=utf-8');
  res.setHeader('Content-Disposition', 'attachment; filename="sales_report.csv"');
  res.send(csv);
});

app.get('/api/export/products-csv', (req, res) => {
  const data = loadData();
  const products = data.products || [];

  let csv = '\uFEFF';
  csv += 'الباركود,اسم المنتج,التصنيف,سعر الشراء,سعر البيع,هامش الربح,الكمية في المخزن,الحد الأدنى\n';

  products.forEach(p => {
    const margin = p.sellPrice - p.buyPrice;
    csv += `"${p.barcode || ''}","${p.name}","${p.category}",${p.buyPrice},${p.sellPrice},${margin},${p.stock},${p.minStock || 5}\n`;
  });

  res.setHeader('Content-Type', 'text/csv; charset=utf-8');
  res.setHeader('Content-Disposition', 'attachment; filename="inventory_stock.csv"');
  res.send(csv);
});

// ----------------- DATABASE BACKUP & RESTORE -----------------

app.get('/api/export', (req, res) => {
  const data = loadData();
  res.setHeader('Content-Disposition', 'attachment; filename="taseer_backup.json"');
  res.setHeader('Content-Type', 'application/json');
  res.send(JSON.stringify(data, null, 2));
});

app.post('/api/import', (req, res) => {
  const importedData = req.body;
  if (!importedData || !Array.isArray(importedData.products)) {
    return res.status(400).json({ error: "ملف البيانات غير صالح" });
  }
  saveData(importedData);
  res.json({ success: true, message: "تم استيراد قاعدة البيانات بنجاح" });
});

app.post('/api/reset-demo', (req, res) => {
  saveData(DEFAULT_DATA);
  res.json({ success: true, message: "تمت استعادة البيانات النموذجية بنجاح" });
});

// Fallback to index.html for SPA
app.get('*', (req, res) => {
  res.sendFile(path.join(__dirname, 'public', 'index.html'));
});

app.listen(PORT, '0.0.0.0', () => {
  console.log(`=========================================`);
  console.log(`  نظام تسيير المبيعات ونقاط البيع الحديث (POS v2.1)`);
  console.log(`  الخادم يعمل على: http://0.0.0.0:${PORT}`);
  console.log(`=========================================`);
});
