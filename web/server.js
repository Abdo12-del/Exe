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
    storeName: "متجر النور للمبيعات",
    phone: "0550 12 34 56",
    address: "شارع الاستقلال، الخروب - قسنطينة",
    currency: "دج",
    taxRate: 0,
    receiptFooter: "شكراً لزيارتكم! البضاعة المباعة ترد أو تستبدل خلال 48 ساعة."
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
  sales: [
    {
      id: 1001,
      invoiceNumber: "INV-2026-0001",
      timestamp: "2026-09-20T08:30:00.000Z",
      dateStr: "2026-09-20 09:30",
      items: [
        { productId: 1, name: "زيت زيتون بكر ممتاز 1 لتر", quantity: 2, sellPrice: 1200, buyPrice: 900, total: 2400 },
        { productId: 8, name: "ماء معدني طبيعي 1.5 لتر", quantity: 6, sellPrice: 50, buyPrice: 35, total: 300 }
      ],
      subtotal: 2700,
      tax: 0,
      discount: 0,
      total: 2700,
      paidAmount: 3000,
      changeAmount: 300,
      profit: 690,
      paymentMethod: "cash"
    },
    {
      id: 1002,
      invoiceNumber: "INV-2026-0002",
      timestamp: "2026-09-20T10:15:00.000Z",
      dateStr: "2026-09-20 11:15",
      items: [
        { productId: 3, name: "تمر دقلة نور أصلي 1 كلغ", quantity: 3, sellPrice: 700, buyPrice: 450, total: 2100 },
        { productId: 2, name: "عسل سدر طبيعي 500 غرام", quantity: 1, sellPrice: 2300, buyPrice: 1600, total: 2300 }
      ],
      subtotal: 4400,
      tax: 0,
      discount: 100,
      total: 4300,
      paidAmount: 4500,
      changeAmount: 200,
      profit: 1350,
      paymentMethod: "cash"
    }
  ],
  nextProductId: 13,
  nextSaleId: 1003
};

function loadData() {
  try {
    if (fs.existsSync(DATA_FILE)) {
      const raw = fs.readFileSync(DATA_FILE, 'utf8');
      return JSON.parse(raw);
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

// ----------------- API Endpoints -----------------

// Settings
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

// Categories
app.get('/api/categories', (req, res) => {
  const data = loadData();
  res.json(data.categories || []);
});

// Products
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

// Process a Sale (Cashier Checkout)
app.post('/api/sales', (req, res) => {
  const data = loadData();
  const { items, discount = 0, paidAmount = 0, paymentMethod = 'cash', customerName = '' } = req.body;

  if (!items || !Array.isArray(items) || items.length === 0) {
    return res.status(400).json({ error: "سلة المشتريات فارغة" });
  }

  // Validate stock and prepare sale items
  let subtotal = 0;
  let totalCost = 0;
  const processedItems = [];

  for (const item of items) {
    const product = data.products.find(p => p.id === item.productId);
    if (!product) {
      return res.status(400).json({ error: `المنتج رقم ${item.productId} غير موجود في المخزون` });
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
  const paid = paidAmount > 0 ? Number(paidAmount) : total;
  const change = Math.max(0, paid - total);

  const saleId = data.nextSaleId++;
  const now = new Date();
  const pad = n => String(n).padStart(2, '0');
  const dateStr = `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())} ${pad(now.getHours())}:${pad(now.getMinutes())}`;

  const sale = {
    id: saleId,
    invoiceNumber: `INV-${now.getFullYear()}-${String(saleId).padStart(4, '0')}`,
    timestamp: now.toISOString(),
    dateStr: dateStr,
    customerName: customerName.trim(),
    items: processedItems,
    subtotal,
    discount: finalDiscount,
    total,
    paidAmount: paid,
    changeAmount: change,
    profit,
    paymentMethod
  };

  data.sales.unshift(sale); // Latest first
  saveData(data);

  res.status(201).json({
    success: true,
    sale,
    message: "تم تسجيل عملية البيع بنجاح وتحديث المخزون"
  });
});

// Sales History
app.get('/api/sales', (req, res) => {
  const data = loadData();
  const { date, q } = req.query;
  let list = data.sales || [];

  if (date) {
    list = list.filter(s => s.dateStr && s.dateStr.startsWith(date));
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

// Stats & Dashboard Analytics
app.get('/api/stats', (req, res) => {
  const data = loadData();
  const products = data.products || [];
  const sales = data.sales || [];

  const today = new Date().toISOString().slice(0, 10);
  let todaySalesTotal = 0;
  let todaySalesCount = 0;
  let todayProfit = 0;

  let totalSalesAmount = 0;
  let totalProfit = 0;

  // Product sales counter
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

  // Top selling products
  const topProducts = Object.entries(productSalesCount)
    .map(([name, count]) => ({ name, count }))
    .sort((a, b) => b.count - a.count)
    .slice(0, 6);

  // Inventory value & low stock
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

  // Recent 7 days timeline
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
      averageTicket: sales.length ? Math.round(totalSalesAmount / sales.length) : 0
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

// Reset demo data
app.post('/api/reset-demo', (req, res) => {
  saveData(DEFAULT_DATA);
  res.json({ success: true, message: "تمت استعادة البيانات النموذجية بنجاح" });
});

// Export Database
app.get('/api/export', (req, res) => {
  const data = loadData();
  res.setHeader('Content-Disposition', 'attachment; filename="taseer_backup.json"');
  res.setHeader('Content-Type', 'application/json');
  res.send(JSON.stringify(data, null, 2));
});

// Import Database
app.post('/api/import', (req, res) => {
  const importedData = req.body;
  if (!importedData || !Array.isArray(importedData.products)) {
    return res.status(400).json({ error: "ملف البيانات غير صالح" });
  }
  saveData(importedData);
  res.json({ success: true, message: "تم استيراد قاعدة البيانات بنجاح" });
});

// Fallback to index.html for SPA
app.get('*', (req, res) => {
  res.sendFile(path.join(__dirname, 'public', 'index.html'));
});

app.listen(PORT, '0.0.0.0', () => {
  console.log(`=========================================`);
  console.log(`  نظام تسيير المبيعات ونقاط البيع الحديث`);
  console.log(`  الخادم يعمل على: http://0.0.0.0:${PORT}`);
  console.log(`=========================================`);
});
