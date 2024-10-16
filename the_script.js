// Add event listener for F9 key to open the dialog
document.addEventListener("keydown", (event) => {
    if (event.key === "F9") {
        document.getElementById("SEB_Hijack").showModal();
    }
});

// Create the dialog element
const dialog = document.createElement("dialog");

// Add content to the dialog
dialog.innerHTML = `
    <h2>SEB Hijack</h2>
    <input type='text' id='urlInput' placeholder='Enter URL' required>
    <button id='openUrlButton'>Open URL</button>
    <button id='exitSEB'>Crash SEB</button>
    <button id='closeButton'>Close</button>
`;

// Set the dialog ID
dialog.id = "SEB_Hijack";

// Append the dialog to the body
document.body.appendChild(dialog);

// Create and append a style element for styling
const style = document.createElement("style");
style.textContent = `
    dialog {
        border: none;
        border-radius: 5px;
        box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
        padding: 20px;
    }
    #urlInput {
        width: calc(100% - 22px);
        padding: 5px;
        margin-right: 5px;
    }
`;
document.head.appendChild(style);

// Add event listener to close the dialog
document.getElementById("closeButton").addEventListener("click", () => {
    document.getElementById("SEB_Hijack").close();
});

// Add event listener to handle button click
document.getElementById("openUrlButton").addEventListener("click", () => {
    const url = document.getElementById("urlInput").value;
    window.open(url, "_blank");
    dialog.close();
});

// Add event listener to crash SEB
document.getElementById("exitSEB").onclick = function () {
    CefSharp.PostMessage({ type: "exitSEB" });
};
