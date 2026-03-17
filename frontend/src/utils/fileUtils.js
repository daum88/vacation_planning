/**
 * Triggers a browser download from a blob response.
 * @param {Blob|ArrayBuffer} data - the response data
 * @param {string} filename - the download filename
 */
export function downloadBlob(data, filename) {
  const url = window.URL.createObjectURL(new Blob([data]));
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  a.remove();
  window.URL.revokeObjectURL(url);
}
