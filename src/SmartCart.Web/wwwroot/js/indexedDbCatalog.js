const databaseName = "SmartCartBrowserData";
const databaseVersion = 2;
const productStoreName = "products";
const shoppingStoreName = "shoppingItems";
let databasePromise;

function openDatabase() {
    if (!databasePromise) {
        databasePromise = new Promise((resolve, reject) => {
            const request = indexedDB.open(databaseName, databaseVersion);
            request.onupgradeneeded = () => {
                const database = request.result;
                if (!database.objectStoreNames.contains(productStoreName)) {
                    const store = database.createObjectStore(productStoreName, { keyPath: "id" });
                    store.createIndex("normalizedName", "normalizedName", { unique: false });
                }
                if (!database.objectStoreNames.contains(shoppingStoreName)) {
                    database.createObjectStore(shoppingStoreName, { keyPath: "id" });
                }
            };
            request.onsuccess = () => resolve(request.result);
            request.onerror = () => reject(request.error ?? new Error("Could not open local product storage."));
            request.onblocked = () => reject(new Error("Local product storage is blocked by another open app tab."));
        });
    }

    return databasePromise;
}

async function transact(storeName, mode, action) {
    const database = await openDatabase();
    return new Promise((resolve, reject) => {
        const transaction = database.transaction(storeName, mode);
        let result;
        transaction.oncomplete = () => resolve(result);
        transaction.onerror = () => reject(transaction.error ?? new Error("Local product storage transaction failed."));
        transaction.onabort = () => reject(transaction.error ?? new Error("Local product storage transaction was cancelled."));
        try {
            result = action(transaction.objectStore(storeName));
        } catch (error) {
            transaction.abort();
            reject(error);
        }
    });
}

async function getAll(storeName) {
    const database = await openDatabase();
    return new Promise((resolve, reject) => {
        const transaction = database.transaction(storeName, "readonly");
        const request = transaction.objectStore(storeName).getAll();
        request.onsuccess = () => resolve(request.result ?? []);
        request.onerror = () => reject(request.error ?? new Error("Could not read local products."));
        transaction.onabort = () => reject(transaction.error ?? new Error("Local product read was cancelled."));
    });
}

export function getAllProducts() {
    return getAll(productStoreName);
}

export function saveProduct(product) {
    return transact(productStoreName, "readwrite", store => store.put(product));
}

export function replaceAllProducts(products) {
    return transact(productStoreName, "readwrite", store => {
        store.clear();
        for (const product of products) {
            store.put(product);
        }
    });
}

export function deleteProduct(id) {
    return transact(productStoreName, "readwrite", store => store.delete(id));
}

export function clearProducts() {
    return transact(productStoreName, "readwrite", store => store.clear());
}

export function getShoppingItems() {
    return getAll(shoppingStoreName);
}

export function saveShoppingItem(item) {
    return transact(shoppingStoreName, "readwrite", store => store.put(item));
}

export function deleteShoppingItem(id) {
    return transact(shoppingStoreName, "readwrite", store => store.delete(id));
}

export function clearShoppingItems() {
    return transact(shoppingStoreName, "readwrite", store => store.clear());
}

export function downloadJson(json, fileName) {
    const link = document.createElement("a");
    const url = URL.createObjectURL(new Blob([json], { type: "application/json" }));
    link.href = url;
    link.download = fileName;
    link.click();
    setTimeout(() => URL.revokeObjectURL(url), 0);
}
