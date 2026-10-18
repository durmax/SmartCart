const maxImageDimension = 2600;

export async function recognizeReceipt(streamReference, expectedContentType, progressReference) {
    const imageBytes = await streamReference.arrayBuffer();
    const bytes = new Uint8Array(imageBytes, 0, Math.min(imageBytes.byteLength, 8));
    const isJpeg = bytes[0] === 0xff && bytes[1] === 0xd8 && bytes[2] === 0xff;
    const isPng = bytes.length === 8 && bytes[0] === 0x89 && bytes[1] === 0x50 &&
        bytes[2] === 0x4e && bytes[3] === 0x47 && bytes[4] === 0x0d &&
        bytes[5] === 0x0a && bytes[6] === 0x1a && bytes[7] === 0x0a;

    if ((expectedContentType === "image/jpeg" && !isJpeg) ||
        (expectedContentType === "image/png" && !isPng) ||
        (!isJpeg && !isPng)) {
        throw new Error("The selected file is not a valid JPEG or PNG image.");
    }

    const source = new Blob([imageBytes], { type: expectedContentType });
    let bitmap;
    let canvas;
    let worker;

    try {
        bitmap = await createImageBitmap(source);
        const scale = Math.min(1, maxImageDimension / Math.max(bitmap.width, bitmap.height));
        let image = source;

        if (scale < 1) {
            canvas = document.createElement("canvas");
            canvas.width = Math.round(bitmap.width * scale);
            canvas.height = Math.round(bitmap.height * scale);
            const context = canvas.getContext("2d", { alpha: false });
            if (!context) {
                throw new Error("This browser could not prepare the receipt image.");
            }

            context.drawImage(bitmap, 0, 0, canvas.width, canvas.height);
            image = await new Promise((resolve, reject) => {
                canvas.toBlob(blob => blob ? resolve(blob) : reject(new Error("Could not resize the receipt image.")), "image/jpeg", 0.9);
            });
        }

        const assetUrl = path => new URL(path, document.baseURI).href;
        worker = await window.Tesseract.createWorker("deu", 1, {
            workerPath: assetUrl("ocr/worker.min.js"),
            corePath: assetUrl("ocr/tesseract-core-lstm.wasm.js"),
            langPath: assetUrl("ocr/tessdata/"),
            workerBlobURL: false,
            gzip: true,
            logger: message => {
                void progressReference.invokeMethodAsync("ReportOcrProgress", message.status, message.progress)
                    .catch(() => {});
            }
        });

        const result = await worker.recognize(image);
        return result.data.text;
    } finally {
        bitmap?.close();
        if (canvas) {
            canvas.width = 0;
            canvas.height = 0;
        }
        if (worker) {
            await worker.terminate().catch(() => {});
        }
    }
}

export function clearReceiptSelection() {
    const input = document.getElementById("receipt-image");
    if (input) {
        input.value = "";
    }
}
