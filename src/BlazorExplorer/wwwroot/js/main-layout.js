// see: https://blog.ppedv.de/post/Blazor-Navbar-Top-Menu-auf-Bootstrap-Basis

// resharper disable all

// these functions are used by the MainLayout.razor
// to automate / integrate Bootstrap menues

function mainLayoutOpenDropDown(event) {
    if (event) {
        event.preventDefault();
        event.stopPropagation();
    }

    var parent = this.parentNode;
    var menu = parent ? parent.querySelector(':scope > .dropdown-menu') : null;
    if (!parent || !menu) {
        return;
    }

    var shouldOpen = !menu.classList.contains("show");
    mainLayoutCleanDropDown(parent);

    parent.classList.toggle("show", shouldOpen);
    menu.classList.toggle("show", shouldOpen);
    this.setAttribute("aria-expanded", shouldOpen ? "true" : "false");

    if (shouldOpen && parent.parentNode && parent.parentNode.classList.contains("navbar-nav")) {
        var rect = this.getBoundingClientRect();
        var left = Math.max(8, Math.min(rect.left, window.innerWidth - 420));
        menu.style.setProperty("--aasx-menu-left", left + "px");
        menu.scrollTop = 0;
    }
}

function mainLayoutCleanDropDown(keepBranch) {
    var dropdowns = document.getElementsByClassName("dropdown-menu");
    for (var i = 0; i < dropdowns.length; i++) {
        var openDropdown = dropdowns[i];
        if (keepBranch && keepBranch.contains(openDropdown)) {
            continue;
        }
        if (openDropdown.classList.contains('show')) {
            openDropdown.classList.remove('show');
        }
    }

    var parents = document.getElementsByClassName("dropdown");
    for (var j = 0; j < parents.length; j++) {
        var parent = parents[j];
        if (keepBranch && keepBranch.contains(parent)) {
            continue;
        }
        parent.classList.remove('show');
    }
}

// the following will add a callback to the GENERAL BROWSER WINDOW !!

window.onclick = function (event) {
    if (!event.target.closest('.dropdown')) {
        mainLayoutCleanDropDown();
    }
}

window.mainLayoutAttachHandlers = () => {
    var elements = document.getElementsByClassName('dropdown-toggle');
    for (var i = 0; i < elements.length; i++) {
        // alert("Attach " + elements[i].id);
        elements[i].addEventListener("click", mainLayoutOpenDropDown, false);
    }
}

// some more code to influence the existing menu

window.setNavBarItem = (id, title) => {
    var anchor_by_id = document.getElementById(id);
    if (anchor_by_id) {
        anchor_by_id.innerText = "" + title;
    }
}

window.setNavBarItemChecked = (id, checked) => {
    var icon = document.getElementById(id);
    if (icon) {
        icon.innerText = checked ? "X" : "";
        icon.classList.toggle("checked", !!checked);
    }
}