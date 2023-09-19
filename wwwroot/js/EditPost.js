var simplemde = new SimpleMDE({
    element: document.getElementById("tbe"), // ~/Post/Edit only!!
    toolbar: [
        "heading-1", "heading-2", "heading-3", "|",
        "bold", "italic", "strikethrough", "|",
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
        enabled: false,
        uniqueId: "EditPost",
        delay: 15000 // Delay between saves, in ms. Default: 10s.
    },
    spellChecker: false,
    indentWithTabs: false
});

const maxLen = 20_000;
simplemde.codemirror.on("change", function (instance) {
    var text = instance.getValue();
    if (text.length > maxLen) {
        // Truncate if exceeds maximum length
        simplemde.value(text.substring(0, maxLen));
    }
});
simplemde.codemirror.on("keydown", function (instance, event) {
    var text = instance.getValue();
    // Note: use of ">=" here is not a typo!
    if (text.length >= maxLen && event.keyCode != 8 && event.keyCode != 46 && (event.keyCode < 37 || event.keyCode > 40)) {
        // If length > maxLen and the key is not the backspace key (keyCode 8) or delete key (46) or arrow key (37-40),
        // prevent the default key behavior
        event.preventDefault();
    }
});
