// ==UserScript==
// @name     Jeff Test
// @version  1
// @include     https://twitter.com/*
// @grant    none
// ==/UserScript==

(function() {
  
  var addLinks = function() {
    
    var accountGroup = document.getElementsByClassName("time");
    
    for(var i=0; i<accountGroup.length; i++) {
    	addLinkToAccount(accountGroup[i]);
    }
    
    
  };
  
  var getTwitterAccountFromTime = function(el) {
  
    var anchor = el.parentElement.firstElementChild;
    var url = anchor.getAttribute('href');
    
    return url.substring(1);
  
  }
  
  var addLinkToAccount = function(el) {
  
  	var newLink = document.createElement('a');
    
		var twitterAccount = getTwitterAccountFromTime(el);
    
    newLink.setAttribute("href", "https://bing.com?id=" + twitterAccount);
    newLink.textContent = "Create Private List";
    el.parentNode.insertBefore(newLink, el.nextSibling);
  
  };
  
  addLinks();
  
})();