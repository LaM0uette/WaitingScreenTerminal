window.consoleScroller = {
    toBottom: function (id) {
        const el = document.getElementById(id);
        if (el) el.scrollTop = el.scrollHeight;
    }
}