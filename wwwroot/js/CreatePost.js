var simplemde = new SimpleMDE({
    element: document.getElementById("tb"),
    toolbar: [
        "heading-2", "heading-3", "|",
        "bold", "italic", "|",
        "quote", "code", "|",
        "unordered-list", "ordered-list", "|",
        "link", "image", "|",
        "preview"
    ],
    insertTexts: {
        image: ["![](http://", ")"],
        link: ["[", "](http://)"]
    },
    autoDownloadFontAwesome: true,
    autosave: {
        enabled: true,
        uniqueId: "CreatePost",
        delay: 30000, // Auto-save time in ms (default: 10s)
    },
    spellChecker: false,
});
