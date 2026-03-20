// see: https://blog.ppedv.de/post/Blazor-Navbar-Top-Menu-auf-Bootstrap-Basis

// resharper disable all

// Custom dropdown toggles (no data-bs-toggle — avoids fighting Bootstrap).
// Supports: same toggle click closes; nested submenus; siblings close when opening another.

function mainLayoutCleanDropDown() {
    var menus = document.querySelectorAll('.dropdown-menu.show');
    for (var i = 0; i < menus.length; i++) {
        var menu = menus[i];
        menu.classList.remove('show');
        var li = menu.parentElement;
        if (li) {
            li.classList.remove('show');
            var tg = li.querySelector(':scope > a.dropdown-toggle');
            if (tg) tg.setAttribute('aria-expanded', 'false');
        }
    }
}

function mainLayoutOpenDropDown(evt) {
    if (evt) {
        if (typeof evt.preventDefault === 'function') evt.preventDefault();
        if (typeof evt.stopPropagation === 'function') evt.stopPropagation();
    }

    var toggle = this;
    var itemLi = toggle.parentElement;
    if (!itemLi) return;

    var menu = itemLi.querySelector(':scope > ul.dropdown-menu');
    if (!menu) return;

    var wasOpen = menu.classList.contains('show');

    if (wasOpen) {
        menu.classList.remove('show');
        itemLi.classList.remove('show');
        toggle.setAttribute('aria-expanded', 'false');
        var nestedMenus = menu.querySelectorAll('.dropdown-menu.show');
        for (var n = 0; n < nestedMenus.length; n++) {
            var nm = nestedMenus[n];
            nm.classList.remove('show');
            var nli = nm.parentElement;
            if (nli) {
                nli.classList.remove('show');
                var nt = nli.querySelector(':scope > a.dropdown-toggle');
                if (nt) nt.setAttribute('aria-expanded', 'false');
            }
        }
        return;
    }

    var parentList = itemLi.parentElement;
    if (parentList && parentList.classList.contains('dropdown-menu')) {
        var childItems = parentList.children;
        for (var c = 0; c < childItems.length; c++) {
            var sib = childItems[c];
            if (sib === itemLi) continue;
            if (!sib.classList.contains('dropdown')) continue;
            var sm = sib.querySelector(':scope > ul.dropdown-menu');
            if (sm && sm.classList.contains('show')) {
                sm.classList.remove('show');
                sib.classList.remove('show');
                var st = sib.querySelector(':scope > a.dropdown-toggle');
                if (st) st.setAttribute('aria-expanded', 'false');
            }
        }
    } else {
        var navbar = toggle.closest('.navbar-nav')
            || toggle.closest('#navbarSupportedContent')
            || toggle.closest('.navbar');
        if (navbar) {
            var roots = navbar.querySelectorAll(':scope > li.nav-item.dropdown');
            for (var r = 0; r < roots.length; r++) {
                var rootLi = roots[r];
                if (rootLi === itemLi) continue;
                var rm = rootLi.querySelector(':scope > ul.dropdown-menu');
                if (rm && rm.classList.contains('show')) {
                    rm.classList.remove('show');
                    rootLi.classList.remove('show');
                    var rt = rootLi.querySelector(':scope > a.dropdown-toggle');
                    if (rt) rt.setAttribute('aria-expanded', 'false');
                }
            }
        }
    }

    menu.classList.add('show');
    itemLi.classList.add('show');
    toggle.setAttribute('aria-expanded', 'true');
}

// Fake event for calls from Blazor (no DOM Event).
function mainLayoutFakeToggleEvent() {
    return {
        preventDefault: function () { },
        stopPropagation: function () { }
    };
}

// Toggle one navbar submenu by id (Blazor); avoids duplicate DOM listeners / event order issues.
window.mainLayoutToggleDropDownById = function (toggleId) {
    var toggle = document.getElementById(toggleId);
    if (!toggle || !toggle.classList.contains('dropdown-toggle')) return;
    mainLayoutOpenDropDown.call(toggle, mainLayoutFakeToggleEvent());
};

window.onclick = function (event) {
    var t = event.target;
    var el = t && t.nodeType === 3 ? t.parentElement : t;
    if (el && el.closest && el.closest('.dropdown-toggle')) return;
    mainLayoutCleanDropDown();
};

// Navbar: Blazor → mainLayoutToggleDropDownById. Do not hook all .dropdown-toggle (flyouts use Bootstrap).
window.mainLayoutAttachHandlers = function () { };

window.setNavBarItem = (id, title) => {
    var anchor_by_id = document.getElementById(id);
    if (anchor_by_id) {
        anchor_by_id.innerText = '' + title;
    }
};
