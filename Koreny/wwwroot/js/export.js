const printRootId = "hourglass-print-root";
const printStyleId = "hourglass-print-style";

/**
 * @param {string} svgContent Full SVG document markup
 * @param {string} filename Suggested download file name
 */
/**
 * @param {string} content File body (UTF-8)
 * @param {string} filename Suggested download file name
 * @param {string} [mimeType] MIME type for the Blob
 */
export function downloadText(content, filename, mimeType) {
  const blob = new Blob([content], { type: mimeType || "text/plain;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = filename && filename.length > 0 ? filename : "export.txt";
  a.rel = "noopener";
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}

export function downloadSvg(svgContent, filename) {
  const blob = new Blob([svgContent], { type: "image/svg+xml;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = filename && filename.length > 0 ? filename : "diagram.svg";
  a.rel = "noopener";
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}

/**
 * Injects the SVG into a print-only root and uses @media print so the rest of the page is hidden.
 * @param {string} svgContent Full SVG document markup
 */
export function printSvg(svgContent) {
  let root = document.getElementById(printRootId);
  if (!root) {
    root = document.createElement("div");
    root.id = printRootId;
    document.body.appendChild(root);
  }
  root.innerHTML = svgContent;

  let styleEl = document.getElementById(printStyleId);
  if (!styleEl) {
    styleEl = document.createElement("style");
    styleEl.id = printStyleId;
    // Hide the whole document in print, then show only the injected tree (screen layout unchanged).
    styleEl.textContent = `
@media print {
  body * { visibility: hidden !important; }
  #${printRootId}, #${printRootId} * { visibility: visible !important; }
  #${printRootId} { position: absolute; left: 0; top: 0; width: 100%; }
}
`;
    document.head.appendChild(styleEl);
  }

  const onAfterPrint = () => {
    root.innerHTML = "";
    window.removeEventListener("afterprint", onAfterPrint);
  };
  window.addEventListener("afterprint", onAfterPrint);
  window.print();
}
