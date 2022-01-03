var pages = [{
    Header: "Online privacy",
    Text: "We secure all your activities like banking, communication, shopping, navigation...",
    Image: "images/FTU-gfx-screen-1.png",
    Button: "Next",
}, {
    Header: "Traveling",
    Text: "Be anywhere. Your access to any content is guaranteed by Avira Phantom VPN.",
    Image: "images/FTU-gfx-screen-2.png",
    Button: "Next",
}, {
    Header: "Public WiFi",
    Text: "Avira Phatnom VPN covers your back by using industry leader encryption to secure all your trafic.",
    Image: "images/FTU-gfx-screen-3.png",
    Button: "Let's go!",
}];

var currentPage = 0;
var previousPage = 0;
var skipTrialPage = false;

function initPages(json, trialDisabled) {    
    pages = JSON.parse(json);
    skipTrialPage = trialDisabled;

    slider(currentPage, previousPage);
    initializeUI(pages[currentPage]);
}

function initializeUI(page) {
    document.getElementById("pageHeader").innerText = page.Header;
    document.getElementById("pageText").innerText = page.Text;
    document.getElementById("pageImage").src = page.Image;
    document.getElementById("pageButton").innerText = page.Button;
}


document.getElementById("trial").style.display = 'none';

function next() {

    if (currentPage === 2) {
        trialFlow();
    }

    previousPage = currentPage;
    currentPage += 1;
    if (currentPage > 2) {
        currentPage = 0;
    }

    slider(currentPage, previousPage);
    initializeUI(pages[currentPage]);
}

function slider(current, previous) {
    removeClass(document.getElementById("slider").children[previous], 'active');
    addClass(document.getElementById("slider").children[current], 'active');
}

function hasClass(ele, cls) {
    return ele.className.match(new RegExp('(\\s|^)' + cls + '(\\s|$)'));
}

function addClass(ele, cls) {
    if (!hasClass(ele, cls)) ele.className += " " + cls;
}

function removeClass(ele, cls) {
    if (hasClass(ele, cls)) {
        var reg = new RegExp('(\\s|^)' + cls + '(\\s|$)');
        ele.className = ele.className.replace(reg, ' ');
    }
}

function trialFlow() {

    if (skipTrialPage) {        
        window.external.ExecuteAction("OpenGui");
    }
    document.getElementById("ftu").style.display = 'none';
    document.getElementById("trial").style.display = 'block';
}