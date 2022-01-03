(function e(t,n,r){function s(o,u){if(!n[o]){if(!t[o]){var a=typeof require=="function"&&require;if(!u&&a)return a(o,!0);if(i)return i(o,!0);var f=new Error("Cannot find module '"+o+"'");throw f.code="MODULE_NOT_FOUND",f}var l=n[o]={exports:{}};t[o][0].call(l.exports,function(e){var n=t[o][1][e];return s(n?n:e)},l,l.exports,e,t,n,r)}return n[o].exports}var i=typeof require=="function"&&require;for(var o=0;o<r.length;o++)s(r[o]);return s})({1:[function(require,module,exports){
"use strict";

function createDummyElement(text, options) {
  var element = document.createElement('div');
  var textNode = document.createTextNode(text);
  element.appendChild(textNode);
  element.style.fontFamily = options.font;
  element.style.fontSize = options.fontSize;
  element.style.fontWeight = options.fontWeight;
  element.style.position = 'absolute';
  element.style.visibility = 'hidden';
  element.style.left = '-999px';
  element.style.top = '-999px';
  element.style.width = options.width;
  element.style.height = 'auto';
  document.body.appendChild(element);
  return element;
}

function destroyElement(element) {
  element.parentNode.removeChild(element);
}

var cache = {};
Object.defineProperty(exports, "__esModule", {
  value: true
});

exports["default"] = function (text, options) {
  if (options === void 0) {
    options = {};
  }

  var cacheKey = JSON.stringify({
    text: text,
    options: options
  });

  if (cache[cacheKey]) {
    return cache[cacheKey];
  }

  options.font = options.font || 'Times';
  options.fontSize = options.fontSize || '16px';
  options.fontWeight = options.fontWeight || 'normal';
  options.width = options.width || 'auto';
  var element = createDummyElement(text, options);
  var size = {
    width: element.offsetWidth,
    height: element.offsetHeight
  };
  destroyElement(element);
  cache[cacheKey] = size;
  return size;
};

},{}],2:[function(require,module,exports){
'use strict';
/**
 * Copyright Marc J. Schmidt. See the LICENSE file at the top-level
 * directory of this distribution and at
 * https://github.com/marcj/css-element-queries/blob/master/LICENSE.
 */

function _typeof(obj) { "@babel/helpers - typeof"; if (typeof Symbol === "function" && typeof Symbol.iterator === "symbol") { _typeof = function _typeof(obj) { return typeof obj; }; } else { _typeof = function _typeof(obj) { return obj && typeof Symbol === "function" && obj.constructor === Symbol && obj !== Symbol.prototype ? "symbol" : typeof obj; }; } return _typeof(obj); }

(function (root, factory) {
  if (typeof define === "function" && define.amd) {
    define(factory);
  } else if ((typeof exports === "undefined" ? "undefined" : _typeof(exports)) === "object") {
    module.exports = factory();
  } else {
    root.ResizeSensor = factory();
  }
})(typeof window !== 'undefined' ? window : void 0, function () {
  // Make sure it does not throw in a SSR (Server Side Rendering) situation
  if (typeof window === "undefined") {
    return null;
  } // https://github.com/Semantic-Org/Semantic-UI/issues/3855
  // https://github.com/marcj/css-element-queries/issues/257


  var globalWindow = typeof window != 'undefined' && window.Math == Math ? window : typeof self != 'undefined' && self.Math == Math ? self : Function('return this')(); // Only used for the dirty checking, so the event callback count is limited to max 1 call per fps per sensor.
  // In combination with the event based resize sensor this saves cpu time, because the sensor is too fast and
  // would generate too many unnecessary events.

  var requestAnimationFrame = globalWindow.requestAnimationFrame || globalWindow.mozRequestAnimationFrame || globalWindow.webkitRequestAnimationFrame || function (fn) {
    return globalWindow.setTimeout(fn, 20);
  };

  var cancelAnimationFrame = globalWindow.cancelAnimationFrame || globalWindow.mozCancelAnimationFrame || globalWindow.webkitCancelAnimationFrame || function (timer) {
    globalWindow.clearTimeout(timer);
  };
  /**
   * Iterate over each of the provided element(s).
   *
   * @param {HTMLElement|HTMLElement[]} elements
   * @param {Function}                  callback
   */


  function forEachElement(elements, callback) {
    var elementsType = Object.prototype.toString.call(elements);
    var isCollectionTyped = '[object Array]' === elementsType || '[object NodeList]' === elementsType || '[object HTMLCollection]' === elementsType || '[object Object]' === elementsType || 'undefined' !== typeof jQuery && elements instanceof jQuery //jquery
    || 'undefined' !== typeof Elements && elements instanceof Elements //mootools
    ;
    var i = 0,
        j = elements.length;

    if (isCollectionTyped) {
      for (; i < j; i++) {
        callback(elements[i]);
      }
    } else {
      callback(elements);
    }
  }
  /**
  * Get element size
  * @param {HTMLElement} element
  * @returns {Object} {width, height}
  */


  function getElementSize(element) {
    if (!element.getBoundingClientRect) {
      return {
        width: element.offsetWidth,
        height: element.offsetHeight
      };
    }

    var rect = element.getBoundingClientRect();
    return {
      width: Math.round(rect.width),
      height: Math.round(rect.height)
    };
  }
  /**
   * Apply CSS styles to element.
   *
   * @param {HTMLElement} element
   * @param {Object} style
   */


  function setStyle(element, style) {
    Object.keys(style).forEach(function (key) {
      element.style[key] = style[key];
    });
  }
  /**
   * Class for dimension change detection.
   *
   * @param {Element|Element[]|Elements|jQuery} element
   * @param {Function} callback
   *
   * @constructor
   */


  var ResizeSensor = function ResizeSensor(element, callback) {
    //Is used when checking in reset() only for invisible elements
    var lastAnimationFrameForInvisibleCheck = 0;
    /**
     *
     * @constructor
     */

    function EventQueue() {
      var q = [];

      this.add = function (ev) {
        q.push(ev);
      };

      var i, j;

      this.call = function (sizeInfo) {
        for (i = 0, j = q.length; i < j; i++) {
          q[i].call(this, sizeInfo);
        }
      };

      this.remove = function (ev) {
        var newQueue = [];

        for (i = 0, j = q.length; i < j; i++) {
          if (q[i] !== ev) newQueue.push(q[i]);
        }

        q = newQueue;
      };

      this.length = function () {
        return q.length;
      };
    }
    /**
     *
     * @param {HTMLElement} element
     * @param {Function}    resized
     */


    function attachResizeEvent(element, resized) {
      if (!element) return;

      if (element.resizedAttached) {
        element.resizedAttached.add(resized);
        return;
      }

      element.resizedAttached = new EventQueue();
      element.resizedAttached.add(resized);
      element.resizeSensor = document.createElement('div');
      element.resizeSensor.dir = 'ltr';
      element.resizeSensor.className = 'resize-sensor';
      var style = {
        pointerEvents: 'none',
        position: 'absolute',
        left: '0px',
        top: '0px',
        right: '0px',
        bottom: '0px',
        overflow: 'hidden',
        zIndex: '-1',
        visibility: 'hidden',
        maxWidth: '100%'
      };
      var styleChild = {
        position: 'absolute',
        left: '0px',
        top: '0px',
        transition: '0s'
      };
      setStyle(element.resizeSensor, style);
      var expand = document.createElement('div');
      expand.className = 'resize-sensor-expand';
      setStyle(expand, style);
      var expandChild = document.createElement('div');
      setStyle(expandChild, styleChild);
      expand.appendChild(expandChild);
      var shrink = document.createElement('div');
      shrink.className = 'resize-sensor-shrink';
      setStyle(shrink, style);
      var shrinkChild = document.createElement('div');
      setStyle(shrinkChild, styleChild);
      setStyle(shrinkChild, {
        width: '200%',
        height: '200%'
      });
      shrink.appendChild(shrinkChild);
      element.resizeSensor.appendChild(expand);
      element.resizeSensor.appendChild(shrink);
      element.appendChild(element.resizeSensor);
      var computedStyle = window.getComputedStyle(element);
      var position = computedStyle ? computedStyle.getPropertyValue('position') : null;

      if ('absolute' !== position && 'relative' !== position && 'fixed' !== position && 'sticky' !== position) {
        element.style.position = 'relative';
      }

      var dirty = false; //last request animation frame id used in onscroll event

      var rafId = 0;
      var size = getElementSize(element);
      var lastWidth = 0;
      var lastHeight = 0;
      var initialHiddenCheck = true;
      lastAnimationFrameForInvisibleCheck = 0;

      var resetExpandShrink = function resetExpandShrink() {
        var width = element.offsetWidth;
        var height = element.offsetHeight;
        expandChild.style.width = width + 10 + 'px';
        expandChild.style.height = height + 10 + 'px';
        expand.scrollLeft = width + 10;
        expand.scrollTop = height + 10;
        shrink.scrollLeft = width + 10;
        shrink.scrollTop = height + 10;
      };

      var reset = function reset() {
        // Check if element is hidden
        if (initialHiddenCheck) {
          var invisible = element.offsetWidth === 0 && element.offsetHeight === 0;

          if (invisible) {
            // Check in next frame
            if (!lastAnimationFrameForInvisibleCheck) {
              lastAnimationFrameForInvisibleCheck = requestAnimationFrame(function () {
                lastAnimationFrameForInvisibleCheck = 0;
                reset();
              });
            }

            return;
          } else {
            // Stop checking
            initialHiddenCheck = false;
          }
        }

        resetExpandShrink();
      };

      element.resizeSensor.resetSensor = reset;

      var onResized = function onResized() {
        rafId = 0;
        if (!dirty) return;
        lastWidth = size.width;
        lastHeight = size.height;

        if (element.resizedAttached) {
          element.resizedAttached.call(size);
        }
      };

      var onScroll = function onScroll() {
        size = getElementSize(element);
        dirty = size.width !== lastWidth || size.height !== lastHeight;

        if (dirty && !rafId) {
          rafId = requestAnimationFrame(onResized);
        }

        reset();
      };

      var addEvent = function addEvent(el, name, cb) {
        if (el.attachEvent) {
          el.attachEvent('on' + name, cb);
        } else {
          el.addEventListener(name, cb);
        }
      };

      addEvent(expand, 'scroll', onScroll);
      addEvent(shrink, 'scroll', onScroll); // Fix for custom Elements and invisible elements

      lastAnimationFrameForInvisibleCheck = requestAnimationFrame(function () {
        lastAnimationFrameForInvisibleCheck = 0;
        reset();
      });
    }

    forEachElement(element, function (elem) {
      attachResizeEvent(elem, callback);
    });

    this.detach = function (ev) {
      // clean up the unfinished animation frame to prevent a potential endless requestAnimationFrame of reset
      if (!lastAnimationFrameForInvisibleCheck) {
        cancelAnimationFrame(lastAnimationFrameForInvisibleCheck);
        lastAnimationFrameForInvisibleCheck = 0;
      }

      ResizeSensor.detach(element, ev);
    };

    this.reset = function () {
      element.resizeSensor.resetSensor();
    };
  };

  ResizeSensor.reset = function (element) {
    forEachElement(element, function (elem) {
      elem.resizeSensor.resetSensor();
    });
  };

  ResizeSensor.detach = function (element, ev) {
    forEachElement(element, function (elem) {
      if (!elem) return;

      if (elem.resizedAttached && typeof ev === "function") {
        elem.resizedAttached.remove(ev);
        if (elem.resizedAttached.length()) return;
      }

      if (elem.resizeSensor) {
        if (elem.contains(elem.resizeSensor)) {
          elem.removeChild(elem.resizeSensor);
        }

        delete elem.resizeSensor;
        delete elem.resizedAttached;
      }
    });
  };

  if (typeof MutationObserver !== "undefined") {
    var observer = new MutationObserver(function (mutations) {
      for (var i in mutations) {
        if (mutations.hasOwnProperty(i)) {
          var items = mutations[i].addedNodes;

          for (var j = 0; j < items.length; j++) {
            if (items[j].resizeSensor) {
              ResizeSensor.reset(items[j]);
            }
          }
        }
      }
    });
    document.addEventListener("DOMContentLoaded", function (event) {
      observer.observe(document.body, {
        childList: true,
        subtree: true
      });
    });
  }

  return ResizeSensor;
});

},{}],3:[function(require,module,exports){
/* eslint-disable strict */
'use strict';

function _defineProperty(obj, key, value) {
  if (key in obj) {
    Object.defineProperty(obj, key, {
      value: value,
      enumerable: true,
      configurable: true,
      writable: true
    });
  } else {
    obj[key] = value;
  }

  return obj;
}

var OeRequest = require('./lib/oe-request');

var libStorage = require('./lib/storage');

var StorageInLocal = libStorage.StorageInLocal;
var StorageInExtension = libStorage.StorageInExtension;

function OAuth(url, client, secret, storage, logger) {
  var self = this;
  var oe = new OeRequest(url, client, secret);
  var storageToUse;

  if (storage === undefined) {
    storageToUse = isExtensionStorageSupported() ? new StorageInExtension() : new StorageInLocal();
  } else {
    storageToUse = storage;
  }

  var refreshToken = '';
  var accessToken = '';
  var requestDate = Date.now();
  var refreshTimeoutMs = 0;
  var user = '';
  var appId = '';

  var writeToLog = function writeToLog(message) {
    if (typeof logger !== "undefined") {
      logger(message);
    }
  };

  writeToLog("Initializing the OAuth library...");

  function isExtensionStorageSupported() {
    if (window.hasOwnProperty('chrome') && window.chrome.hasOwnProperty('storage') && window.chrome.storage.hasOwnProperty('local')) {
      return true;
    } else {
      return false;
    }
  }

  function isTokenExpired() {
    return refreshTimeoutMs > 0 && Date.now() - requestDate > refreshTimeoutMs;
  }

  function initializeAuthData(json) {
    oe.useOAuthToken(json);
    accessToken = json.access_token;
    refreshToken = json.refresh_token || '';
    refreshTimeoutMs = json.expires_in * 1000 || 0;
    requestDate = json.request_date || Date.now();
    user = json.user || '';
    return accessToken;
  }

  function cleanupContext() {
    refreshToken = '';
    accessToken = '';
    refreshTimeoutMs = 0;
    user = '';
    appId = '';
  }

  function getOEUser(username) {
    return new Promise(function (resolve, reject) {
      storageToUse.get('oe_user').then(function (results) {
        storageToUse.get('oe-avira-token').then(function (aviraToken) {
          if (aviraToken[username]) {
            results.aviraToken = aviraToken[username];
          }

          resolve(results);
        })["catch"](function () {
          resolve(results);
        });
      })["catch"](function () {
        storageToUse.get('oe-avira-token').then(function (aviraToken) {
          if (aviraToken[username]) {
            resolve({
              aviraToken: aviraToken[username]
            });
          }

          reject();
        })["catch"](function () {
          reject();
        });
      });
    });
  }

  function saveAviraToken(username, token) {
    storageToUse.get('oe-avira-token').then(function (tokens) {
      tokens[username] = token;
      storageToUse.set('oe-avira-token', tokens);
    })["catch"](function () {
      storageToUse.set('oe-avira-token', _defineProperty({}, username, token));
    });
  }

  var refreshingToken = false;

  self.refresh = function () {
    if (!isTokenExpired()) {
      return Promise.resolve(accessToken);
    }

    if (!self.isToken()) {
      writeToLog("No Refresh token found. Returning anonym token.");
      return oe.request(oe.anonymous()).then(initializeAuthData);
    }

    writeToLog("Token is expired. Refreshing token...");

    if (self.refreshingToken) {
      return Promise.resolve(accessToken);
    }

    self.refreshingToken = true;
    return oe.request(oe.refresh(refreshToken)).then(function (json) {
      writeToLog("Token Refresh successful. Storing access token. ".concat(JSON.stringify(json)));
      json.user = user || '';
      initializeAuthData(json);
      return storageToUse.set('oe_user', {
        access_token: accessToken,
        refresh_token: refreshToken,
        expires_in: refreshTimeoutMs / 1000,
        request_date: requestDate,
        user: user
      });
    })["catch"](function (error) {
      writeToLog("Failed to refresh token. Error: ".concat(JSON.stringify(error)));
      self.refreshingToken = false;

      if (error.errors && error.errors[0] && error.errors[0].status === "500") {
        return Promise.resolve(accessToken);
      }

      cleanupContext();
      return storageToUse.remove('oe_user');
    }).then(function () {
      self.refreshingToken = false;
      return Promise.resolve(accessToken);
    });
  }; //Returns an anonymous token if the user is not logged in.


  self.token = function () {
    if (accessToken && accessToken !== '') {
      return self.refresh();
    }

    return storageToUse.get('oe_user')["catch"](function () {
      writeToLog("No token found in storage. Requesting anonymous token...");
      return oe.request(oe.anonymous());
    }).then(function (json) {
      writeToLog("Cached or Anonymous token json: ".concat(JSON.stringify(json)));
      initializeAuthData(json);
      return self.refresh();
    });
  };

  self.storeGdprConsent = function () {
    return oe.get('/v2/me').then(function (responce) {
      return oe.put("/v2/users/".concat(responce.data.id), {
        data: {
          attributes: {
            gdpr_consent: new Date().toISOString()
          },
          type: "users",
          id: responce.data.id
        }
      });
    })["catch"](function (e) {
      return Promise.reject("Failed to store Gdpr Consent. Error ".concat(JSON.stringify(e)));
    });
  };

  self.getCurrentUser = function () {
    if (!self.isLoggedIn()) return new Promise(function (resolve, reject) {
      return resolve(null);
    });
    return oe.get('/v2/me');
  }; //Returns empty string if the user is not logged in.


  self.accessToken = function () {
    if (accessToken && accessToken !== '') {
      return self.refresh();
    }

    return storageToUse.get('oe_user').then(function (json) {
      initializeAuthData(json);
      return self.refresh();
    })["catch"](function () {
      writeToLog("User is not logged in. Returning empty token.");
      return '';
    });
  };

  self.isEmailConfirmed = function () {
    return self.accessToken().then(function (token) {
      if (token) {
        return oe.get('/v2/me');
      }

      writeToLog("User is not logged in. Can't check if email is confirmed.");
      return Promise.resolve(false);
    }).then(function (response) {
      return new Promise(function (resolve) {
        return resolve(response.data.attributes.optin === "double_confirmed");
      });
    })["catch"](function (e) {
      return Promise.reject("Failed to check email confirmation status. Error ".concat(JSON.stringify(e)));
    });
  };

  self.logout = function () {
    cleanupContext();
    oe.useOAuthToken("");
    return storageToUse.remove('oe_user');
  };

  self.payments = function (clientId, appid, language) {
    var source = arguments.length > 3 && arguments[3] !== undefined ? arguments[3] : 'product';
    return self.permanentToken(clientId, language).then(function (token) {
      return oe.request(oe.createPaymentUrlApiRequest(token, appid, source));
    })["catch"](function (error) {
      if (error.error === 'expired_token') {
        // something strange here, althrough validity period is not ended, permanent token is expired
        // problem is token will not be refreshed (this token is only expired on OE side)
        // we need to manually refresh it, althrough rethrowing the error to notify client of error
        // next time client will call payments with new token
        requestDate = 0;
        return self.refresh().then(function () {
          throw error;
        });
      }

      throw error;
    });
  };

  self.handleOAuthResponse = function (response, username) {
    initializeAuthData(response);
    user = username || '';
    return storageToUse.set('oe_user', {
      access_token: accessToken,
      refresh_token: refreshToken,
      expires_in: refreshTimeoutMs / 1000,
      request_date: requestDate,
      user: username || ''
    });
  };

  self.loginFromApp = function (user, device, app, otp) {
    return self.logout().then(function () {
      return self.registerDevice(device, app);
    }).then(function () {
      return oe.loginUser(user, otp);
    }).then(function (response) {
      return self.handleOAuthResponse(response, user.email);
    });
  };

  self.registerDevice = function (device, app) {
    if (self.isLoggedIn() || self.appIsRegistered()) {
      return new Promise(function (resolve, reject) {
        return resolve(true);
      });
    }

    writeToLog("Registering device with OE...");
    return oe.requestTempToken().then(function () {
      return oe.createAnonymousUser();
    }).then(function () {
      return oe.createDevice(device);
    }).then(function () {
      return oe.getPermanentToken();
    }).then(function (r) {
      return self.handleOAuthResponse(r);
    }).then(function () {
      return oe.createAppInstance(app);
    }).then(function (r) {
      writeToLog("Successfuly registered device with OE.");
      appId = r.data.id;
    });
  };

  self.registerUserFromApp = function (user, device, app, otp) {
    return oe.requestTempToken().then(function () {
      return oe.createUser(user);
    }).then(function () {
      return self.loginFromApp(user, device, app, otp);
    });
  };

  self.login = function (username, password, captcha, otp) {
    var trustedBrowser = arguments.length > 4 && arguments[4] !== undefined ? arguments[4] : {};

    if (self.isLoggedIn()) {
      return new Promise(function (resolve, reject) {
        return reject({
          error: 'already logged in',
          error_description: ''
        });
      });
    }

    return getOEUser(username)["catch"](function () {
      return oe.request(oe.login(username, password, captcha, otp, trustedBrowser));
    }).then(function (results) {
      // if user was not avialable from the storage
      // OR access_token was not aviabale after the login request from the catch statement
      // then send a login request again else return the results
      if (!results.user && !results.access_token) {
        // if the browser is marked as trusted than do no send the avira-token
        // because the user has some how reached the OTP verification screen
        // and sending a avira-token along with avira token breaks the API.
        if (results.aviraToken && trustedBrowser && !trustedBrowser.trusted) {
          trustedBrowser.token = results.aviraToken;
        }

        return oe.request(oe.login(username, password, captcha, otp, trustedBrowser));
      }

      return results;
    }).then(function (results) {
      initializeAuthData(results);
      user = username;

      if (results.aviraToken && trustedBrowser.trusted) {
        saveAviraToken(username, results.aviraToken);
      }

      return storageToUse.set('oe_user', {
        access_token: accessToken,
        refresh_token: refreshToken,
        expires_in: refreshTimeoutMs / 1000,
        request_date: requestDate,
        user: username || ''
      });
    }).then(function () {
      return self.refresh();
    });
  };

  self.getUserInfo = function () {
    return storageToUse.get('oe_user');
  };

  self.cachedToken = function () {
    return storageToUse.get('oe_user').then(function (json) {
      initializeAuthData(json);
      return new Promise(function (resolve) {
        return resolve(accessToken);
      });
    });
  };

  self.permanentToken = function (clientId, language) {
    if (self.isToken()) {
      return self.refresh();
    }

    var temptoken;
    return self.token().then(function (token) {
      temptoken = token;
      return oe.request(oe.registerAnonymous(token, language));
    }).then(function (response) {
      return oe.request(oe.permanent(temptoken, clientId));
    }).then(function (response) {
      initializeAuthData(response);
      return storageToUse.set('oe_user', {
        access_token: accessToken,
        refresh_token: refreshToken,
        expires_in: refreshTimeoutMs / 1000,
        request_date: requestDate,
        user: ""
      });
    }).then(function () {
      return self.refresh();
    });
  };

  self.isLoggedIn = function () {
    return user != '';
  };

  self.isToken = function () {
    return refreshToken && refreshToken != '';
  };

  self.appIsRegistered = function () {
    return appId != '';
  };

  self.register = function (email, password) {
    return self.token().then(function (token) {
      return oe.request(oe.register(token, email, password));
    });
  };

  self.resetPassword = function (email, captcha) {
    return oe.request(oe.resetPassword(email, captcha));
  };

  self.resendVerificationEmail = function (token) {
    return oe.resendVerificationEmail(token);
  };

  self.sendDevicePing = function (token) {
    return oe.sendDevicePing(token);
  };

  self.getPermanentTokenForSocialMediaToken = function (token, socialMediaHandle) {
    return oe.getPermanentTokenForSocialMediaToken(token, socialMediaHandle).then(function (response) {
      initializeAuthData(response);
      return storageToUse.set('oe_user', {
        access_token: accessToken,
        refresh_token: refreshToken,
        expires_in: refreshTimeoutMs / 1000,
        request_date: requestDate,
        user: ""
      });
    }).then(function () {
      return self.refresh();
    });
  };
}

module.exports = OAuth;

},{"./lib/oe-request":4,"./lib/storage":5}],4:[function(require,module,exports){
"use strict";

function ownKeys(object, enumerableOnly) {
  var keys = Object.keys(object);

  if (Object.getOwnPropertySymbols) {
    var symbols = Object.getOwnPropertySymbols(object);
    if (enumerableOnly) symbols = symbols.filter(function (sym) {
      return Object.getOwnPropertyDescriptor(object, sym).enumerable;
    });
    keys.push.apply(keys, symbols);
  }

  return keys;
}

function _objectSpread(target) {
  for (var i = 1; i < arguments.length; i++) {
    var source = arguments[i] != null ? arguments[i] : {};

    if (i % 2) {
      ownKeys(Object(source), true).forEach(function (key) {
        _defineProperty(target, key, source[key]);
      });
    } else if (Object.getOwnPropertyDescriptors) {
      Object.defineProperties(target, Object.getOwnPropertyDescriptors(source));
    } else {
      ownKeys(Object(source)).forEach(function (key) {
        Object.defineProperty(target, key, Object.getOwnPropertyDescriptor(source, key));
      });
    }
  }

  return target;
}

function _defineProperty(obj, key, value) {
  if (key in obj) {
    Object.defineProperty(obj, key, {
      value: value,
      enumerable: true,
      configurable: true,
      writable: true
    });
  } else {
    obj[key] = value;
  }

  return obj;
}

function OeRequest(url, client, secret) {
  var self = this;
  self.authString = "avira/".concat(client, ":").concat(secret);

  self.createRequest = function (method, requestUrl, authorization, body, otp, trustedBrowser) {
    var headers = {
      Accept: 'application/json',
      'Content-Type': 'application/json'
    };

    if (authorization) {
      headers.Authorization = authorization;
    }

    if (otp) {
      headers['X-Avira-Otp'] = otp;
    }

    if (trustedBrowser && (trustedBrowser.token || trustedBrowser.trusted)) {
      headers['X-Avira-Browser'] = trustedBrowser.browser;
      headers['X-Avira-Os'] = trustedBrowser.os;

      if (trustedBrowser.trusted) {
        headers['X-Avira-Browser-Trusted'] = trustedBrowser.trusted;
      }

      if (trustedBrowser.token) {
        headers['X-Avira-Token'] = trustedBrowser.token;
      }
    }

    var request = {
      method: method,
      redirect: 'follow',
      cache: 'no-cache',
      headers: new Headers(headers)
    };

    if (body) {
      request['body'] = JSON.stringify(body);
    }

    return new Request(requestUrl, request);
  };

  self.createOAuthRequest = function (body, otp, trustedBrowser) {
    return self.createRequest('post', "".concat(url, "/v2/oauth"), "Basic ".concat(btoa(self.authString)), body, otp, trustedBrowser);
  };

  self.createApiRequest = function (token, body) {
    return self.createRequest('post', "".concat(url, "/v2/users"), "Bearer ".concat(token), body);
  };

  self.createPaymentUrlApiRequest = function (token, appid, source) {
    return self.createRequest('get', "".concat(url, "/v2/payment-urls?filter[app]=").concat(appid, "&filter[source]=").concat(source), "Bearer ".concat(token));
  };

  self.createPasswordRequest = function (body) {
    return self.createRequest('post', "".concat(url, "/v2/passwords"), "Basic ".concat(btoa(self.authString)), body);
  };

  self.createPermanentTokenRequest = function (token, body) {
    return self.createRequest('post', "".concat(url, "/v2/oauth"), "Bearer ".concat(token), body);
  };

  self.anonymous = function () {
    return self.createOAuthRequest({
      grant_type: 'client_credentials'
    });
  };

  self.permanent = function (token, clientId) {
    return self.createPermanentTokenRequest(token, {
      grant_type: 'authorization_code',
      client_id: clientId
    });
  };

  var oauth_token = {};
  var app_id = '';
  var aviraPartner = {
    "data": {
      "type": "partners",
      "id": "avira"
    }
  };

  self.useOAuthToken = function (token) {
    return oauth_token = token;
  };

  self.resolvePromise = function (result) {
    return new Promise(function (resolve, refect) {
      resolve(result);
    });
  };

  self.get = function (path) {
    var request = self.createRequest('get', "".concat(url).concat(path), "Bearer ".concat(oauth_token.access_token));
    return self.request(request);
  };

  self.post = function (path, body, otp) {
    var request = self.createRequest('post', "".concat(url).concat(path), "Bearer ".concat(oauth_token.access_token), body, otp);
    return self.request(request);
  };

  self.put = function (path, body) {
    var request = self.createRequest('put', "".concat(url).concat(path), "Bearer ".concat(oauth_token.access_token), body);
    return self.request(request);
  };

  self.requestTempToken = function () {
    return self.request(self.anonymous()).then(function (response) {
      oauth_token = response;
    });
  };

  self.getPermanentToken = function () {
    var body = {
      grant_type: "authorization_code",
      client_id: "launcher"
    };
    return self.post("/v2/oauth", body).then(function (response) {
      oauth_token = response;
      return self.resolvePromise(response);
    });
  };

  self.loginUser = function (user, otp) {
    var body = {
      username: user.email,
      password: user.password,
      grant_type: "password"
    };
    return self.post("/v2/oauth", body, otp).then(function (response) {
      oauth_token = response;
      return self.resolvePromise(response);
    });
  };

  self.createAnonymousUser = function () {
    var body = {
      data: {
        relationships: {
          partner: aviraPartner
        },
        attributes: {
          status_tfa: "inactive"
        },
        type: "users"
      }
    };
    return self.post("/v2/users", body);
  };

  self.createUser = function (user) {
    var body = {
      data: {
        relationships: {
          partner: aviraPartner
        },
        attributes: user,
        type: "users"
      }
    };
    return self.post("/v2/users", body);
  };

  self.createDevice = function (device) {
    var body = {
      data: {
        relationships: {
          "partner": aviraPartner
        },
        attributes: device,
        type: "devices"
      }
    };
    return self.post("/v2/devices", body).then(function (response) {
      self.deviceId = response.data.id;
    });
  };

  self.createAppInstance = function (app_data) {
    var app_instance = {
      data: {
        attributes: app_data,
        relationships: {
          device: {
            data: {
              type: "devices",
              id: self.deviceId
            }
          },
          app: {
            data: {
              type: "apps",
              id: app_data.app_id
            }
          }
        }
      }
    };
    return self.post("/v2/app-instances", app_instance).then(function (response) {
      self.app_id = response.data.id;
      return self.resolvePromise(response);
    });
  };

  self.setAppInstanceStatus = function (status) {
    var app_instance = {
      data: {
        attributes: {
          status: status
        },
        state: "active"
      }
    };
    return self.put("/v2/app-instances/".concat(self.app_id), app_instance);
  };

  self.login = function (username, password, captcha, otp, trustedBrowser) {
    var body = {
      username: username,
      password: password,
      grant_type: 'password'
    };

    if (captcha) {
      body.captcha = captcha;
    }

    return self.createOAuthRequest(body, otp, trustedBrowser);
  };

  self.refresh = function (refreshToken) {
    return self.createOAuthRequest({
      refresh_token: refreshToken,
      client_id: client,
      grant_type: 'refresh_token'
    });
  };

  self.registerAnonymous = function (token, language) {
    return self.createApiRequest(token, {
      data: {
        relationships: {
          partner: aviraPartner
        },
        attributes: {
          language: language
        },
        type: 'users'
      }
    });
  };

  self.register = function (token, email, password) {
    return self.createApiRequest(token, {
      data: {
        relationships: {
          partner: aviraPartner
        },
        attributes: {
          email: email,
          password: password,
          gdpr_consent: new Date().toISOString()
        },
        type: 'users'
      }
    });
  };

  self.resetPassword = function (email) {
    var captcha = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : '';
    return self.createPasswordRequest({
      data: {
        relationships: {
          partner: {
            data: {
              type: 'partners',
              id: 'avira'
            }
          }
        },
        attributes: {
          email: email
        },
        type: 'passwords'
      },
      meta: {
        captcha: captcha
      }
    });
  };

  self.fetchWithRetry = function (requestObject, retries, timeout) {
    var retriesAttempts = retries;
    var delay = timeout || 1000;
    return new Promise(function (resolve) {
      var fetchWrapper = function fetchWrapper(attempts) {
        fetch(requestObject).then(function (response) {
          if (response.status === 400 || response.status === 409) {
            // bypass retry for conflict and bad request statuses
            resolve(response);
          }

          if (!response.ok) {
            throw response;
          }

          resolve(response);
        })["catch"](function (response) {
          if (attempts > 0) {
            setTimeout(function () {
              return fetchWrapper(attempts - 1);
            }, delay);
          } else {
            // http errors as well as exceptions
            resolve(response);
          }
        });
      };

      fetchWrapper(retriesAttempts);
    });
  };

  self.request = function (oeRequest) {
    return self.fetchWithRetry(oeRequest, 0).then(function (response) {
      return new Promise(function (resolve) {
        if (!(response instanceof Response)) {
          resolve({
            ok: false,
            json: {
              errors: [{
                detail: response.toString()
              }]
            }
          });
          return;
        }

        var aviraPhone = response.headers && response.headers.get('X-Avira-Phone');
        var aviraToken = response.headers && response.headers.get('X-Avira-Token');
        response.json().then(function (json) {
          if (aviraPhone) {
            json['aviraPhone'] = aviraPhone;
          }

          if (aviraToken) {
            json['aviraToken'] = aviraToken;
          }

          resolve({
            ok: response.ok,
            json: json
          });
        })["catch"](function () {
          return resolve({
            ok: response.ok,
            json: {
              errors: [{
                status: response.status.toString(),
                detail: response.statusText
              }]
            }
          });
        });
      });
    }).then(function (results) {
      if (!results.ok) {
        throw results.json;
      }

      return results.json;
    });
  };

  self.resendVerificationEmail = function (token) {
    var body = {
      meta: {
        send_verify_email: true
      },
      data: {
        attributes: {},
        // work around against a bug
        relationships: {
          partner: {
            data: {
              type: 'partners',
              id: 'avira'
            }
          }
        }
      }
    };
    var request = self.createRequest('put', "".concat(url, "/v2/me"), "Bearer ".concat(token), body);
    return self.request(request);
  };

  self.sendDevicePing = function (token) {
    var body = {
      data: {
        attributes: {}
      }
    };
    var request = self.createRequest('put', "".concat(url, "/v2/my-device"), "Bearer ".concat(token), body);
    return self.request(request);
  };

  self.getPermanentTokenForSocialMediaToken = function (token, socialMediaHandle, otp, trustedBrowser, redirectURI) {
    var grantTypes = {
      facebook: 'fb_auth',
      google: 'gg_auth',
      apple: 'apl_auth'
    };
    var body = {
      token: token,
      grant_type: grantTypes[socialMediaHandle],
      partner: 'avira'
    };

    if (redirectURI) {
      body = _objectSpread(_objectSpread({}, body), {}, {
        redirect_uri: redirectURI
      });
    }

    return self.request(self.createOAuthRequest(body, otp, trustedBrowser));
  };
}

module.exports = OeRequest;

},{}],5:[function(require,module,exports){
"use strict";

function StorageInExtension() {
  var self = this;

  self.set = function (name, value) {
    return new Promise(function (resolve) {
      var storage = {};
      storage[name] = value;
      chrome.storage.local.set(storage, function () {
        resolve();
      });
    });
  };

  self.get = function (name) {
    return new Promise(function (resolve, reject) {
      chrome.storage.local.get(name, function (storage) {
        var value = storage[name];

        if (value === undefined) {
          reject();
        } else {
          resolve(value);
        }
      });
    });
  };

  self.remove = function (name) {
    return new Promise(function (resolve) {
      chrome.storage.local.remove(name, function (storage) {
        resolve();
      });
    });
  };
}

function StorageInLocal() {
  var self = this;

  self.set = function (name, value) {
    return new Promise(function (resolve) {
      localStorage.setItem(name, JSON.stringify(value));
      resolve();
    });
  };

  self.get = function (name) {
    return new Promise(function (resolve, reject) {
      var stringifiedValue = localStorage.getItem(name);
      var value = JSON.parse(stringifiedValue);

      if (value === null) {
        reject();
      } else {
        resolve(value);
      }
    });
  };

  self.remove = function (name) {
    return new Promise(function (resolve) {
      localStorage.removeItem(name);
      resolve();
    });
  };
}

module.exports = {
  StorageInExtension: StorageInExtension,
  StorageInLocal: StorageInLocal
};

},{}],6:[function(require,module,exports){
"use strict";

var ResizeSensor = require('css-element-queries/src/ResizeSensor');

angular.module('rate5Stars', []).controller('rateController', ['$scope', function ($scope) {
  $scope.frameWidth = 392;
  $scope.frameHeight = 550;
  $scope.marginTop = 100;
  $scope.starsText = "";
  var starsTextArray = [""];
  var selectedStars = 0;
  var modal = document.getElementById('rateModalId');
  var container = document.getElementById('guiFrameContainerId');

  var calculateModalContainerSize = function calculateModalContainerSize() {
    $scope.frameWidth = $('body').width();
    $scope.frameHeight = $('body').height();
  };

  calculateModalContainerSize();

  var calculateMarginTop = function calculateMarginTop() {
    var modalHeight = $scope.showDontShowAgain || !$scope.alignButtonsHorizontally && $scope.selectableStars ? 405 : 385;
    $scope.marginTop = ($scope.frameHeight - modalHeight) / 2;
  };

  calculateMarginTop();
  new ResizeSensor(container, function () {
    $scope.$apply(function () {
      calculateModalContainerSize();
      calculateMarginTop();
    });
  });

  var hideModal = function hideModal() {
    modal.className = "Modal is-hidden is-visuallyHidden";
    $scope.show = false;
  };

  $scope.rate = function (rateClicked) {
    if (rateClicked) {
      if (!$scope.selectableStars || $scope.rating > 0) {
        $scope.onRate()($scope.rating);
        hideModal();
      }
    } else {
      $scope.onNotNow();
      hideModal();
    }

    if ($scope.dontShowAgainChecked) {
      $scope.onDontShowAgain();
    }
  };

  $scope.$watch('show', function (value) {
    if (value) {
      modal.className = "Modal is-visuallyHidden";
      setTimeout(function () {
        modal.className = "Modal clearfix";
      }, 100);
      $scope.updateSelectedStars(0);
      $scope.rating = 0;
      $scope.dontShowAgainChecked = false;
    }
  });

  $scope.getStarImage = function (starIdx) {
    if (!$scope.selectableStars || starIdx <= selectedStars) {
      return "Star";
    } else {
      return "StarGrey";
    }
  };

  $scope.updateSelectedStars = function (starIdx) {
    if ($scope.selectableStars) {
      if (starsTextArray.length < 5) {
        starsTextArray.push($scope.oneStarText);
        starsTextArray.push($scope.twoStarsText);
        starsTextArray.push($scope.threeStarsText);
        starsTextArray.push($scope.fourStarsText);
        starsTextArray.push($scope.fiveStarsText);
      }

      selectedStars = starIdx;
      $scope.starsText = starsTextArray[starIdx];
    }
  };

  $scope.updateRating = function (starIdx) {
    if ($scope.selectableStars) {
      $scope.rating = starIdx;
    }
  };
}]);

},{"css-element-queries/src/ResizeSensor":2}],7:[function(require,module,exports){
"use strict";

angular.module('rate5Stars').directive('rate', function () {
  return {
    templateUrl: 'widgets/rate-5stars/rate.html',
    restrict: 'E',
    replace: true,
    scope: {
      rateTitleText: '@',
      rateDescriptionText: '@',
      rateButtonText: '@',
      notNowButtonText: '@',
      selectableStars: '=',
      oneStarText: '@',
      twoStarsText: '@',
      threeStarsText: '@',
      fourStarsText: '@',
      fiveStarsText: '@',
      showDontShowAgain: '=',
      dontShowAgainText: '@',
      onDontShowAgain: '&',
      onRate: '&',
      onNotNow: '&',
      show: '=',
      alignButtonsHorizontally: '='
    },
    controller: 'rateController'
  };
});

},{}],8:[function(require,module,exports){
"use strict";

function whenShowsUp(selector, callback) {
  var i = setInterval(function () {
    if ($(selector).length > 0) {
      clearInterval(i);
      callback($(selector));
    }
  }, 100);
}

var external = require('services/vpn-external');

if (external.isSimulator) {
  var VpnSimulator = {
    traffic: 0,
    limit: 1002345552,
    connected: false,
    grace_period: 60,
    language: "en-US",
    license: {
      LicenseType: "Registered",
      //"Registered", "Unregistered", "Pro"
      expiration_date: new Date(Date.now()),
      subscription: false,
      traffic_limit_interval: "monthly"
    },
    userRegistered: "true",
    wifi: [{
      Id: "def",
      Ssid: "Flughafeb-Munchen-Free",
      Autoconnect: false,
      Connected: true
    }, {
      Id: "gsa",
      Ssid: "avira-tt-guest",
      Autoconnect: false,
      Connected: false
    }, {
      Id: "gsa2",
      Ssid: "avira-tt-guest",
      Autoconnect: false,
      Connected: false
    }],
    productCatalogue: {
      productCatalogue: [{
        id: "avpp27",
        price: "5 USD",
        period: "monthly",
        registrationNeeded: false,
        trial: false
      }, {
        id: "avpp28",
        price: "30 USD",
        period: "yearly",
        registrationNeeded: false,
        trial: false
      }, {
        id: "avpp29",
        price: "8.99 USD",
        period: "monthly",
        registrationNeeded: true,
        trial: false
      }, {
        id: "avpp30",
        price: "60 USD",
        period: "yearly",
        registrationNeeded: true,
        trial: false
      }, {
        id: "avpp31",
        price: "5 USD",
        period: "monthly",
        registrationNeeded: false,
        trial: true
      }]
    },
    start: function start() {
      var self = this;
      var i = 0;
      external.testScriptingObject.RegisterRequestCallback(function (message) {
        switch (message.method) {
          case 'users/currentUser/license':
            external.testScriptingObject.respond("users/currentUser/license", {
              result: self.license
            });
            break;

          case 'uiLanguage/get':
            external.testScriptingObject.respond("uiLanguage/get", {
              result: self.language
            });
            break;

          case 'appSettings/get':
            external.testScriptingObject.respond("appSettings/get", {
              result: {
                "appImprovement": false,
                "showFastFeedback": true
              }
            });
            break;

          case 'disconnect':
          case 'connect':
            self.connected = !self.connected;
            external.testScriptingObject.respond("status", {
              result: {
                status: self.connected ? "Connected" : "Disconnected"
              }
            });
            self.updateStatus(self.connected ? "Connected" : "Disconnected");
            break;

          case 'status':
            external.testScriptingObject.respond("status", {
              result: {
                status: self.connected ? "Connected" : "Disconnected"
              }
            });
            break;

          case 'isSandBoxed/get':
            external.testScriptingObject.respond("isSandBoxed/get", {
              result: "True"
            });
            break;

          case 'productCatalogue':
            self.updateProductCatalogue(self.productCatalogue);
            break;

          case 'refreshIPAddress':
            if (self.connected) {
              self.ipAddressRefreshed({
                ip: "201.55.22.222"
              });
            } else {
              self.ipAddressRefreshed({
                ip: "10.45.23.123"
              });
            }

            break;

          case 'upgrade':
            self.license.LicenseType = "Pro";
            self.updateLicense(self.license);
            break;

          case 'userProfile':
            external.testScriptingObject.respond('userProfile/get', {
              result: {
                first_name: 'Glong name with more ch',
                last_name: 'aracters'
              }
            });
            break;

          case 'purchase':
            self.license.LicenseType = "Pro";
            self.updatePuchaseStatus("purchasing");
            setTimeout(function () {
              self.updatePuchaseStatus("purchased");
              self.license.LicenseType = "Pro";
              self.updateLicense(self.license);
            }, 3000);
            /* Uncomment this for fail case
            setTimeout(() => { 
                self.updatePuchaseStatus("failed");
            }, 3000);*/

            break;

          case 'restorePurchase':
            self.updatePuchaseStatus("restoring");
            /*setTimeout(() => { 
                self.updatePuchaseStatus("failed");
            }, 3000);*/

            break;

          case 'registerUser':
            self.license.LicenseType = 'Registered';
            self.updateLicense(self.license);
            break;

          case 'openDashboard':
            self.license.LicenseType = 'Unregistered';
            self.updateLicense(self.license);
            break;

          case 'userRegistered':
            self.updateUserRegister(self.userRegistered);
            break;

          case 'wifis/get':
            console.log("=======> Achtung!");
            self.updateWifi(self.wifi);
            break;

          case 'storageGet':
            var response = "";

            if (message.params === "eula_accepted") {
              response = {
                accepted: true
              };
            } else if (message.params === "unconfirmed_email") {
              response = {
                email: "someemail@gmail.com"
              };
            } else if (message.params === "keychain_page_disabled") {
              response = {
                disabled: false
              };
            } else if (message.params === "fast_feedback") {
              response = {
                show: true
              };
            }

            external.testScriptingObject.respond("storageGet", {
              result: response
            });
            break;

          case 'wifis/trust':
            for (var i = 0; i < self.wifi.length; i++) {
              if (self.wifi[i].Id == message.params) {
                self.wifi[i].Trusted = true;
              }
            }

            console.log(JSON.stringify(self.wifi));
            break;

          case 'wifis/untrust':
            for (i = 0; i < self.wifi.length; i++) {
              if (self.wifi[i].Id == message.params) {
                self.wifi[i].Trusted = false;
              }
            }

            console.log(JSON.stringify(self.wifi));
            break;

          case 'features/get':
            external.testScriptingObject.respond("features/get", {
              result: {
                wifiManagement: true,
                trial: true,
                killSwitch: true,
                udpSupport: true,
                disableTracking: true,
                malwareProtection: {
                  enabled: true,
                  beta: true
                },
                adBlocking: {
                  enabled: true,
                  beta: true
                },
                fastFeedback: {
                  enabled: true
                },
                waitingWindow: {
                  enabled: true,
                  params: {
                    connect_timeout: 5
                  }
                },
                diagnosticTool: {
                  enabled: true
                },
                ipAddress: {
                  enabled: true
                }
              }
            });
            break;

          case 'deviceData/get':
            external.testScriptingObject.respond("deviceData/get", {
              result: {
                "name": "TestSimulator",
                "alias": "",
                "agent_version": "1.2.85.9999",
                "date_updated": null,
                "brand": "Avira",
                "hardware_id": "a9f2b677cfc46decc5fe3def0e22c1b729722622",
                "date_churned": null,
                "state": "pending",
                "os": "windows",
                "others": {},
                "date_added": "2016-09-19T11:45:58Z",
                "model": "X700",
                "country": "IT",
                "type": "pc",
                "hidden": false,
                "os_version": "1.0.1234",
                "download_source": "wd",
                "agent_language": "en",
                "os_type": "desktop",
                "locked": false,
                "tracking_id": "58fb4855eb1ef"
              }
            });
            break;

          case 'appData/get':
            external.testScriptingObject.respond("appData/get", {
              result: {
                download_source: "bc",
                bundle_id: "",
                app_id: "avpn0"
              }
            });
            break;

          case 'wifis/delete':
            var idx = -1;

            for (i = 0; i < self.wifi.length; i++) {
              if (self.wifi[i].Id == message.params) {
                idx = i;
              }
            }

            if (idx != -1) {
              self.wifi.splice(idx, 1);
            }

            console.log(JSON.stringify(self.wifi));
            external.testScriptingObject.respond("wifis/delete", {
              result: {}
            });
            break;

          case 'fastFeedbackStrings':
            external.testScriptingObject.respond("fastFeedbackStrings", {
              result: {
                title: "Rate Avira Phantom VPN",
                description: "How would you rate your experience with Avira Phantom VPN ?",
                button_submit: "Submit",
                button_cancel: "Not now",
                dontShowAgainText: "Don't show again",
                ratings: {
                  one: "Very Bad",
                  two: "Bad",
                  three: "Average",
                  four: "Good",
                  five: "Very Good"
                }
              }
            });
            break;

          case 'diagnostics/send':
            setTimeout(function () {
              self.uploadDiagnosticData('fd2f-482b-b35a');
            }, 3000);
            external.testScriptingObject.respond("diagnostics/send", {
              result: {
                "result": true
              }
            });
            break;

          case 'diagnostics/lastReference':
            external.testScriptingObject.respond("diagnostics/lastReference", {
              result: {
                id: "fd2f-482b-b35a",
                date: "Wednesday 12th April 2019"
              }
            });
            break;

          case 'displaySettings/get':
            external.testScriptingObject.respond("displaySettings/get", {
              result: {
                OsSettings: true
              }
            });
            break;

          case 'themeSelection/get':
            external.testScriptingObject.respond("themeSelection/get", {
              result: {
                displayed: true
              }
            });
            break;

          case 'systemSettings':
            external.testScriptingObject.respond("systemSettings", {
              result: {
                theme: "DarkTheme"
              }
            });
            break;

          case 'login':
            self.loginResult('NoValidSubscriptions');
            break;
        }
      });
      i = setInterval(function () {
        if (!self.connected) {
          return false;
        }

        self.traffic += 172631;

        if (self.traffic > self.limit) {
          external.testScriptingObject.mimicReceive({
            message: {
              method: "trafficLimitReached",
              params: {}
            }
          });
          clearInterval(i);
        }

        self.updateTraffic();
      }, 1000); //
      // "method": "appSettings/get", 

      external.testScriptingObject.respond("traffic/get", {
        result: {
          used: self.traffic,
          limit: self.limit,
          grace_period: self.grace_period
        }
      });
      external.testScriptingObject.respond("regions/get", {
        result: {
          "default": "de",
          timestamp: 0,
          ttl: 7200.0,
          regions: [{
            id: "de",
            name: "Germany",
            host: "de2.shield.avira.com",
            port: 443,
            protocol: "tcp",
            latency: "150 ms",
            license_type: 'paid'
          }, {
            id: "uk",
            name: "United Kingdom",
            host: "de2.shield.avira.com",
            port: 443,
            protocol: "tcp",
            latency: "300 ms",
            license_type: 'free'
          }, {
            id: "us",
            name: "США — Вашингтон, округ Колумбия",
            host: "us2.shield.avira.com",
            port: 443,
            protocol: "tcp",
            latency: "301 ms",
            license_type: 'paid'
          }, {
            id: "us_1",
            name: "USA 1",
            host: "us2.shield.avira.com",
            port: 443,
            protocol: "tcp",
            latency: "abcdef",
            license_type: 'paid'
          }, {
            id: "us_2",
            name: "USA 2",
            host: "us2.shield.avira.com",
            port: 443,
            protocol: "tcp",
            license_type: 'paid'
          }, {
            id: "us_3",
            name: "USA 3",
            host: "us2.shield.avira.com",
            port: 443,
            protocol: "tcp",
            license_type: 'paid'
          }, {
            id: "de_1",
            name: "Germany 1",
            host: "de2.shield.avira.com",
            port: 443,
            protocol: "tcp"
          }, {
            id: "us_4",
            name: "USA 4",
            host: "us2.shield.avira.com",
            port: 443,
            protocol: "tcp",
            license_type: 'paid'
          }, {
            id: "de_2",
            name: "Germany 2",
            host: "de2.shield.avira.com",
            port: 443,
            protocol: "tcp"
          }, {
            id: "us_5",
            name: "USA 5",
            host: "us2.shield.avira.com",
            port: 443,
            protocol: "tcp"
          }, {
            id: "de_3",
            name: "Germany3",
            host: "de2.shield.avira.com",
            port: 443,
            protocol: "tcp"
          }]
        }
      });
    },
    updateTraffic: function updateTraffic() {
      external.testScriptingObject.mimicReceive({
        message: {
          method: "traffic/get",
          params: {
            used: this.traffic,
            limit: this.limit,
            grace_period: this.grace_period
          }
        }
      });
    },
    sendError: function sendError(message) {
      external.testScriptingObject.mimicReceive({
        message: {
          method: "error",
          params: {
            message: message
          }
        }
      });
    },
    updateLicense: function updateLicense(license) {
      external.testScriptingObject.mimicReceive({
        message: {
          method: "users/currentUser/license",
          params: license
        }
      });
    },
    updateWifi: function updateWifi(wifi) {
      external.testScriptingObject.mimicReceive({
        message: {
          method: "wifis/get",
          params: wifi
        }
      });
    },
    updatePuchaseStatus: function updatePuchaseStatus(status) {
      external.testScriptingObject.mimicReceive({
        message: {
          method: "purchaseStatus",
          params: {
            "status": status
          }
        }
      });
    },
    updateUserRegister: function updateUserRegister(userRegistered) {
      external.testScriptingObject.mimicReceive({
        message: {
          method: "userRegistered",
          params: userRegistered
        }
      });
    },
    updateUiVisible: function updateUiVisible(uiVisible) {
      external.testScriptingObject.mimicReceive({
        message: {
          method: "uiVisible",
          params: {
            "isVisible": uiVisible
          }
        }
      });
    },
    updateProductCatalogue: function updateProductCatalogue(productCatalogue) {
      external.testScriptingObject.mimicReceive({
        message: {
          method: "productCatalogue",
          params: productCatalogue
        }
      });
    },
    displayRateMeDialog: function displayRateMeDialog() {
      external.testScriptingObject.mimicReceive({
        message: {
          method: "displayRateMe"
        }
      });
    },
    displayFastFeedbackDialog: function displayFastFeedbackDialog() {
      external.testScriptingObject.mimicReceive({
        message: {
          method: "displayFastFeedback"
        }
      });
    },
    displayDataUsagePopup: function displayDataUsagePopup() {
      external.testScriptingObject.mimicReceive({
        message: {
          method: "displayDataUsagePopup"
        }
      });
    },
    uploadDiagnosticData: function uploadDiagnosticData(reference) {
      external.testScriptingObject.mimicReceive({
        message: {
          method: "diagnostics/get",
          params: {
            id: reference
          }
        }
      });
    },
    updateStatus: function updateStatus(status) {
      if (!status) {
        return;
      }

      this.connected = status.toLowerCase() === "connected";
      external.testScriptingObject.mimicReceive({
        message: {
          method: "status",
          params: {
            status: status
          }
        }
      });
    },
    ipAddressRefreshed: function ipAddressRefreshed(ipData) {
      external.testScriptingObject.mimicReceive({
        message: {
          method: "ipAddressRefreshed",
          params: ipData
        }
      });
    },
    loginResult: function loginResult(errorResponse) {
      external.testScriptingObject.mimicReceive({
        message: {
          method: "login",
          params: {
            error: errorResponse
          }
        }
      });
    },
    uiSettingsChanged: function uiSettingsChanged(uiSettings) {
      external.testScriptingObject.mimicReceive({
        message: {
          method: "uiSettingsChanged",
          params: uiSettings
        }
      });
    }
  };
  window.VPN = VpnSimulator;
  VpnSimulator.start();
}

},{"services/vpn-external":56}],9:[function(require,module,exports){
module.exports={"label": "phantom"}
},{}],10:[function(require,module,exports){
"use strict";

module.exports = function (app) {
  app.directive('bindHtmlUnsafe', function () {
    function link(scope, element, attrs) {
      var update = function update() {
        element.html(scope.html);
      };

      attrs.$observe('bindHtmlUnsafe', function (value) {
        update();
      });
    }

    return {
      link: link,
      scope: {
        html: '='
      }
    };
  });
};

},{}],11:[function(require,module,exports){
"use strict";

/*! Angular clickout v1.0.2 | � 2014 Greg Berg� | License MIT */
module.exports = function (app) {
  app.directive('clickOut', ['$window', '$parse', function ($window, $parse) {
    return {
      restrict: 'A',
      link: function link(scope, element, attrs) {
        var clickOutHandler = $parse(attrs.clickOut);
        angular.element($window).on('click', function (event) {
          if (element[0].contains(event.target)) return;
          clickOutHandler(scope, {
            $event: event
          });
          scope.$apply();
        });
      }
    };
  }]);
};

},{}],12:[function(require,module,exports){
"use strict";

module.exports = function (app) {
  app.directive('collectDiagnosticData', function () {
    return {
      templateUrl: 'views/directives/collect_diagnostic_data.html',
      replace: true,
      scope: {},
      controller: ['$scope', 'MessageBus', 'gettextCatalog', 'DiagnosticData', function ($scope, MessageBus, gettextCatalog, DiagnosticData) {
        $scope.isOptionSelected = function () {
          return $scope.connectionIssueChecked || $scope.speedIssueChecked || $scope.licenseIssueChecked || $scope.otherIssueChecked;
        };

        $scope.descriptionUpdated = function () {
          if ($scope.description != "") {
            $scope.otherIssueChecked = true;
          } else {
            $scope.otherIssueChecked = false;
          }
        };

        MessageBus.on(MessageBus.VPN_CHANGEVIEW, function (newView) {
          if (newView == "collectDiagnosticDataView") {
            $scope.connectionIssueChecked = false;
            $scope.speedIssueChecked = false;
            $scope.licenseIssueChecked = false;
            $scope.otherIssueChecked = false;
            $scope.description = "";
          }
        });

        $scope.next = function () {
          if ($scope.isOptionSelected()) {
            DiagnosticData.userSelection.connectionIssue = $scope.connectionIssueChecked;
            DiagnosticData.userSelection.speedIssue = $scope.speedIssueChecked;
            DiagnosticData.userSelection.licenseIssue = $scope.licenseIssueChecked;
            DiagnosticData.userSelection.otherIssue = $scope.otherIssueChecked;
            DiagnosticData.userSelection.description = $scope.description;
            MessageBus.trigger(MessageBus.VPN_VIEW, "sentDiagnosticDataView", "mainView");
          }
        };
      }]
    };
  });
};

},{}],13:[function(require,module,exports){
"use strict";

module.exports = function (app) {
  app.directive('confirmSentData', function () {
    return {
      templateUrl: 'views/directives/confirm_sent_data.html',
      replace: true,
      scope: {},
      controller: ['$scope', '$timeout', 'MessageBus', 'DiagnosticData', function ($scope, $timeout, MessageBus, DiagnosticData) {
        $scope.showCopyRefButton = true;
        MessageBus.on(MessageBus.VPN_CHANGEVIEW, function (newView) {
          if (newView == "confirmSentDataView") {
            $scope.reference = DiagnosticData.reference;
          }
        });

        $scope.done = function () {
          MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, "mainView");
        };

        $scope.copyRefClicked = function () {
          var copyElement = document.getElementById("refNumber");
          var range = document.createRange();
          range.selectNode(copyElement);
          window.getSelection().removeAllRanges();
          window.getSelection().addRange(range);
          document.execCommand('copy');
          window.getSelection().removeAllRanges();
          $scope.showCopyRefButton = false;
          $timeout(function () {
            $scope.showCopyRefButton = true;
          }, 3000);
        };
      }]
    };
  });
};

},{}],14:[function(require,module,exports){
"use strict";

var ResizeSensor = require('css-element-queries/src/ResizeSensor');

module.exports = function (app) {
  app.directive('datausagepopup', function () {
    return {
      templateUrl: 'views/directives/data_usage_popup.html',
      replace: true,
      scope: {},
      controller: ['$scope', 'MessageBus', 'gettextCatalog', function ($scope, MessageBus, gettextCatalog) {
        $scope.traffic = {
          used: 0,
          limit: 0
        };
        $scope.frameWidth = 392;
        $scope.frameHeight = 550;
        $scope.marginTop = 100;
        var modal = document.getElementById('dataUsagePopupModalId');
        var container = document.getElementById('guiFrameContainerId');

        var calculateModalContainerSize = function calculateModalContainerSize() {
          $scope.frameWidth = $('body').width();
          $scope.frameHeight = $('body').height();
        };

        calculateModalContainerSize();

        var calculateMarginTop = function calculateMarginTop() {
          var modalHeight = 429;
          $scope.marginTop = ($scope.frameHeight - modalHeight) / 2;
        };

        calculateMarginTop();
        new ResizeSensor(container, function () {
          $scope.$apply(function () {
            calculateModalContainerSize();
            calculateMarginTop();
          });
        });

        var hideModal = function hideModal() {
          modal.className = "ModalDataUsagePopup is-hidden is-visuallyHidden";
        };

        var progressCircle = function progressCircle(progress) {
          return {
            "fill": "rgb(218, 219, 220)",
            // ring-grey 
            "stroke": "rgb(59, 172, 252)",
            // ring-blue (91, 171, 245)
            "stroke-width": "32",
            "stroke-dasharray": "".concat(progress, " 100"),
            "height": "100px"
          };
        };

        $scope.notNowClicked = function () {
          MessageBus.request(MessageBus.NOT_NOW_DATA_USAGE);
          hideModal();
        };

        $scope.getProButtonClicked = function () {
          MessageBus.request(MessageBus.UPGRADE);
          MessageBus.request(MessageBus.GET_PRO_DATA_USAGE);
          hideModal();
        };

        var getPercentUsed = function getPercentUsed() {
          if ($scope.traffic.limit == 0) return 0;
          var percentUsed = $scope.traffic.used * 100 / $scope.traffic.limit;
          return Math.round(percentUsed);
        };

        MessageBus.subscribe(MessageBus.TRAFFIC, function (message) {
          try {
            $scope.traffic = message.params;
          } catch (error) {}
        });
        MessageBus.request(MessageBus.GETTRAFFIC, function (message) {
          try {
            $scope.traffic = message.params;
          } catch (error) {}
        });

        var formatBytes = function formatBytes(bytes) {
          if (bytes === 0) {
            return gettextCatalog.getString('0 bytes');
          }

          var thresh = 1024;

          if (Math.abs(bytes) < thresh) {
            return bytes + ' B';
          }

          var units = ['KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'];
          var u = -1;

          do {
            bytes /= thresh;
            ++u;
          } while (Math.abs(bytes) >= thresh && u < units.length - 1);

          return bytes.toFixed(2) + ' ' + units[u];
        };

        MessageBus.subscribe(MessageBus.DISPLAY_DATA_USAGE_POPUP, function () {
          $scope.dataUsed = formatBytes($scope.traffic.used);

          if ($scope.traffic.limit == 0) {
            $scope.dataRemaining = formatBytes(0);
          } else {
            $scope.dataRemaining = formatBytes($scope.traffic.limit - $scope.traffic.used);
          }

          $scope.progressValue = getPercentUsed();
          $scope.progressCircle = progressCircle($scope.progressValue);
          modal.className = "ModalDataUsagePopup is-visuallyHidden";
          setTimeout(function () {
            modal.className = "ModalDataUsagePopup clearfix";
          }, 100);
        });
      }]
    };
  });
};

},{"css-element-queries/src/ResizeSensor":2}],15:[function(require,module,exports){
"use strict";

module.exports = function (app) {
  app.directive('diagnostics', function () {
    return {
      templateUrl: 'views/directives/diagnostics.html',
      replace: true,
      scope: {},
      controller: ['$scope', 'MessageBus', 'gettextCatalog', 'Telemetry', 'Configurator', function ($scope, MessageBus, gettextCatalog, Telemetry, Configurator) {
        var appSettings = {
          appImprovement: false,
          killSwitch: false,
          udpSupport: false,
          autoSecureUntrustedWifi: false,
          malwareProtection: false,
          adBlocking: false
        };
        MessageBus.request(MessageBus.APPSETTINGSGET, function (message) {
          appSettings = message.result;
        });

        var setAllowSendDiagData = function setAllowSendDiagData(enabled) {
          appSettings.appImprovement = enabled;
          MessageBus.request(MessageBus.APPSETTINGSSET, appSettings, function (message) {});
        };

        var showThemeSelectionIfNeeded = function showThemeSelectionIfNeeded() {
          if (Configurator.showThemeSelection) {
            MessageBus.request(MessageBus.THEME_SELECTION_GET, function (message) {
              MessageBus.trace("Theme selection settings : " + JSON.stringify(message));
              var themeSelection = message.result;

              if (themeSelection.displayed) {
                MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, "mainView");
                MessageBus.trigger(MessageBus.VPN_ENABLE_HEADER);
                return;
              }

              MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, "themeSelectionView");
            });
          }
        };

        $scope.send = function (accepted) {
          setAllowSendDiagData(accepted);

          if (accepted) {
            Telemetry.sendEvent(Telemetry.INSTALL_SUCCESS);
          }

          showThemeSelectionIfNeeded();
        };
      }]
    };
  });
};

},{}],16:[function(require,module,exports){
"use strict";

module.exports = function (app) {
  app.directive('displaySettings', function () {
    return {
      templateUrl: 'views/directives/display_settings.html',
      replace: true,
      scope: {},
      controller: ['$scope', '$rootScope', 'MessageBus', 'Configurator', function ($scope, $rootScope, MessageBus, Configurator) {
        $scope.display = {
          OsSettings: true,
          LightTheme: false,
          DarkTheme: false
        };
        MessageBus.on(MessageBus.VPN_CHANGEVIEW, function (newView) {
          $scope.view = newView;
        });

        var broadcastTheme = function broadcastTheme(theme) {
          MessageBus.trace("Setting application theme to : " + theme);
          MessageBus.request("themeColor", theme, function () {});
          $rootScope.$broadcast("theme", theme);
        };

        var setThemeFromOs = function setThemeFromOs() {
          if (Configurator.useOsTheme) {
            MessageBus.appHostRequest(MessageBus.SYSTEM_SETTINGS, null, function (message) {
              var settings = message.result;
              broadcastTheme(settings.theme);
            });
          }
        };

        var setTheme = function setTheme(theme) {
          if (theme === "OsSettings") {
            setThemeFromOs();
          } else {
            broadcastTheme(theme);
          }
        };

        var subscribeForThemeChanges = function subscribeForThemeChanges() {
          if (Configurator.useOsTheme) {
            MessageBus.subscribe(MessageBus.SYSTEM_SETTINGS_CHANGED, function (message) {
              var settings = message ? message.params : null;

              if (settings != null && $scope.display.OsSettings === true) {
                broadcastTheme(settings.theme);
              }
            });
          }
        };

        subscribeForThemeChanges();
        MessageBus.request(MessageBus.DISPLAY_SETTINGS_GET, function (message) {
          MessageBus.trace("Display settings received : " + JSON.stringify(message));
          var displaySettings = message.result;
          Object.keys(displaySettings).forEach(function (key) {
            $scope.display[key] = displaySettings[key];

            if (displaySettings[key]) {
              setTheme(key);
            }
          });
        });

        $scope.updateDisplaySettings = function ($event) {
          var target = $event.currentTarget.id;
          console.log(JSON.stringify($scope.display)); // if selection was active, dont turn it off on second click, reset to active

          if ($scope.display[target] === false) {
            $scope.display[target] = true;
            return;
          } // only one of the settings is allowed. Turn off others


          Object.keys($scope.display).forEach(function (key) {
            if (key !== target) {
              $scope.display[key] = false;
            }
          });
          MessageBus.request(MessageBus.DISPLAY_SETTINGS_SET, $scope.display, function () {});
          setTheme(target);
        };

        MessageBus.on(MessageBus.VPN_CHANGE_THEME, function (theme) {
          var displaySettings = {
            OsSettings: false,
            LightTheme: false,
            DarkTheme: false
          };

          if (theme === "LightTheme") {
            displaySettings.LightTheme = true;
          } else if (theme === "DarkTheme") {
            displaySettings.DarkTheme = true;
          } else {
            displaySettings.OsSettings = true;
          }

          $scope.display = displaySettings;
          MessageBus.request(MessageBus.DISPLAY_SETTINGS_SET, $scope.display, function () {});
          setTheme(theme);
        });
      }]
    };
  });
};

},{}],17:[function(require,module,exports){
"use strict";

module.exports = function (app) {
  app.directive('emailConfirmation', function () {
    return {
      templateUrl: 'views/directives/email_confirmation.html',
      replace: true,
      scope: {},
      controller: ['$scope', 'MessageBus', 'AppHostStorage', 'gettextCatalog', 'oe', function ($scope, MessageBus, AppHostStorage, gettextCatalog, oe) {
        $scope.loggedin = false;
        $scope.isEmailResent = false;
        $scope.registeredEmail = "";

        var getUnconfirmedEmail = function getUnconfirmedEmail() {
          AppHostStorage.get("unconfirmed_email").then(function (data) {
            if (data && JSON.stringify(data) !== '{}') {
              $scope.$apply(function () {
                $scope.registeredEmail = data.email;
              });
            }
          });
        };

        MessageBus.on(MessageBus.VPN_CHANGEVIEW, function (newView) {
          if (newView == "emailConfirmationView") {
            getUnconfirmedEmail();
            $scope.isEmailResent = false;
          }
        });

        $scope.backButtonClicked = function () {
          oe.isEmailConfirmed().then(function (isConfirmed) {
            MessageBus.trigger(MessageBus.VPN_UPDATE_REGISTRATION_STATUS, isConfirmed);
          });
          MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, "mainView");
        };

        $scope.resendEmail = function () {
          oe.token().then(function (token) {
            return oe.resendVerificationEmail(token);
          }).then(function (result) {
            $scope.$apply(function () {
              $scope.isEmailResent = true;
            });
          })["catch"](function (error) {
            MessageBus.trace("Failed to resent confimation email. " + JSON.stringify(error));
          });
        };

        $scope.wrongEmail = function () {
          oe.logout();
          MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, "registerView");
          MessageBus.trigger(MessageBus.VPN_USER_REGISTERED, false);
        };

        MessageBus.on(MessageBus.VPN_USER_REGISTERED, function (loggedin) {
          $scope.loggedin = loggedin;
        });
      }]
    };
  });
};

},{}],18:[function(require,module,exports){
"use strict";

var isMacClient = window.MacAppController !== undefined;

module.exports = function (app) {
  app.directive('features', function () {
    return {
      templateUrl: 'views/directives/features.html',
      replace: true,
      scope: {},
      controller: ['$scope', '$timeout', 'MessageBus', 'RegionList', 'gettextCatalog', 'Telemetry', 'Configurator', 'License', function ($scope, $timeout, MessageBus, regionList, gettextCatalog, Telemetry, Configurator, License) {
        $scope.licenseType = License.getLicenseType();
        $scope.features = {};
        $scope.region = regionList.selected;
        $scope.isMacClient = isMacClient;
        $scope.proBadgeLabel = Configurator.getLabels().ProBadge;
        $scope.showAboutLink = Configurator.showAboutLink;
        $scope.productName = Configurator.getLabels().ProductName;
        $scope.showQuitLink = Configurator.showQuitLink && $scope.isMacClient;
        $scope.showLogoutLink = Configurator.showLogoutLink;
        $scope.showThemeSelection = Configurator.showThemeSelection;
        var isSandBoxed = false;
        $scope.appSettings = {
          appImprovement: true,
          killSwitch: false,
          udpSupport: false,
          autoSecureUntrustedWifi: false,
          malwareProtection: false,
          adBlocking: false
        };
        $scope.userSettings = {
          autoStart: false
        };

        var initNanoScrollBar = function initNanoScrollBar() {
          $(".nano").nanoScroller();
          $(".nano-pane").css("display", "block");
          $(".nano-slider").css("display", "block");
        };

        initNanoScrollBar();

        $scope.countryButtonClicked = function () {
          MessageBus.trigger(MessageBus.VPN_VIEW, "regionsView", "settingsView");
        };

        $scope.wifiButtonClicked = function () {
          MessageBus.trigger(MessageBus.VPN_VIEW, "wifiView", "settingsView");
        };

        $scope.displaySettingsClicked = function () {
          MessageBus.trigger(MessageBus.VPN_VIEW, "displaySettingsView", "settingsView");
        };

        $scope.upgradeButtonClicked = function () {
          Telemetry.sendEvent(Telemetry.UPGRADE_CLICKED, {
            "UI Button": "Features"
          });
          MessageBus.request(MessageBus.UPGRADE);
        };

        $scope.updateSettings = function () {
          MessageBus.request(MessageBus.APPSETTINGSSET, $scope.appSettings, function (message) {});
          MessageBus.appHostRequest(MessageBus.USERSETTINGSSET, $scope.userSettings);
        };

        $scope.updateProFeature = function (feature) {
          if ($scope.licenseType === "Pro") {
            $scope.updateSettings();
            return;
          }

          $timeout(function () {
            $scope.appSettings[feature] = false;
          }, 400);
          $scope.upgradeButtonClicked();
        };

        MessageBus.on(MessageBus.VPN_CHANGEVIEW, function (newView) {
          $scope.view = newView;

          if (newView == "settingsView") {
            setTimeout(function () {
              $('.nano').nanoScroller();
            }, 0);
          }
        });
        MessageBus.request(MessageBus.FEATURES, function (message) {
          $scope.features = message.result;
        });
        MessageBus.on(MessageBus.VPN_FEATURES, function (features) {
          $scope.features = features;
        });
        MessageBus.on(MessageBus.VPN_IS_SANDBOXED, function (message) {
          isSandBoxed = message;
        });

        $scope.getSettings = function () {
          MessageBus.request(MessageBus.APPSETTINGSGET, function (message) {
            //trigger new status
            MessageBus.trace("App settings received " + JSON.stringify(message));
            $scope.appSettings = message.result;
          });
          MessageBus.appHostRequest(MessageBus.USERSETTINGSGET, null, function (message) {
            //trigger new status
            MessageBus.trace("User settings received " + JSON.stringify(message));
            $scope.userSettings = message.result;
          });
        };

        MessageBus.subscribe(MessageBus.APPSETTINGS, function () {
          $scope.getSettings();
        });
        $scope.getSettings();

        $scope.quit = function () {
          MessageBus.appHostRequest(MessageBus.QUIT);
        };

        MessageBus.on(MessageBus.SELECTED_REGION_CHANGED, function (r) {
          $scope.region = r;
        });

        var disableProFeatures = function disableProFeatures() {
          var sendRequest = false;

          if ($scope.appSettings.killSwitch === true) {
            $scope.appSettings.killSwitch = false;
            sendRequest = true;
          }

          if ($scope.appSettings.autoSecureUntrustedWifi === true) {
            $scope.appSettings.autoSecureUntrustedWifi = false;
            sendRequest = true;
          }

          if (sendRequest === true) {
            MessageBus.request(MessageBus.APPSETTINGSSET, $scope.appSettings, function (message) {});
          }
        };

        if ($scope.licenseType !== "Pro") {
          disableProFeatures();
        }

        MessageBus.on(MessageBus.VPN_LICENSE_CHANGED, function () {
          $scope.licenseType = License.getLicenseType();
          $scope.getSettings();

          if ($scope.licenseType !== "Pro") {
            disableProFeatures();
          }
        });
        MessageBus.subscribe("connectionReestablished", function () {
          requestFeatures();
          $scope.updateSettings();
        });

        var calculateNanoSize = function calculateNanoSize() {
          var borderSize = 2; //px

          return $('body').height() - 67
          /*settings header*/
          - $('#about').height() - borderSize - $('#header').outerHeight();
        };

        $scope.$watch(function () {
          try {
            if ($scope.view != "settingsView") return;
            return calculateNanoSize();
          } catch (error) {
            return 0;
          }
        }, function () {
          if ($scope.view != "settingsView") return;
          $('.nano').height(calculateNanoSize);
          $timeout(function () {
            $(".nano").nanoScroller();
          }, 1);
        });

        $scope.aboutVpnClicked = function () {
          MessageBus.appHostRequest(MessageBus.OPEN_URL_IN_DEFAULT_BROWSER, {
            url: Configurator.aboutUrl("")
          }, function () {});
        };

        $scope.logout = function () {
          MessageBus.request("logout");
        };
      }]
    };
  });
};

},{}],19:[function(require,module,exports){
"use strict";

module.exports = function (app) {
  app.directive('forcedLogin', function () {
    return {
      templateUrl: 'views/directives/forced_login.html',
      replace: true,
      scope: {},
      controller: ['$scope', 'MessageBus', 'Configurator', 'gettextCatalog', '$timeout', function ($scope, MessageBus, Configurator, gettextCatalog, $timeout) {
        var authenticatorError = {
          SUCCESS: 'Success',
          UNKNOWN_ERROR: 'UnknownError',
          INVALID_USERNAME_OR_PASSWORD: 'InvalidUsernameOrPassword',
          NO_VALID_SUBSCRIPTIONS: 'NoValidSubscriptions'
        };
        $scope.emailPlaceholder = gettextCatalog.getString('Email address');
        $scope.passwordPlaceholder = gettextCatalog.getString('Password');
        $scope.isLoading = true;
        $scope.email_error = "";
        $scope.password_error = "";

        $scope.forgotPasswordClicked = function () {
          MessageBus.appHostRequest(MessageBus.OPEN_URL_IN_DEFAULT_BROWSER, {
            url: Configurator.login_forgotPasswordUrl
          }, function () {});
        };

        function handleUserRegistered(status) {
          var loggedin = status === "true";
          MessageBus.trace("User login status :" + loggedin);
          MessageBus.trace("Forced login status : " + Configurator.useForcedLogin);

          if (loggedin) {
            MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, "mainView");
          } else {
            $scope.isLoading = false;
          }
        }

        $scope.registerClicked = function () {
          MessageBus.appHostRequest(MessageBus.OPEN_URL_IN_DEFAULT_BROWSER, {
            url: Configurator.login_registerUrl
          }, function () {});
        };

        function validate() {
          $scope.email_error = $scope.loginForm.email.$valid ? "" : gettextCatalog.getString('Enter a valid email address first');
          $scope.password_error = $scope.loginForm.password.$valid ? "" : gettextCatalog.getString('Enter a valid password');
          return $scope.loginForm.email.$valid && $scope.loginForm.password.$valid;
        }

        function switchToMainView() {
          MessageBus.trigger(MessageBus.VPN_LICENSE_CHANGED);
          MessageBus.trigger(MessageBus.VPN_USER_REGISTERED, true);
          MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, "mainView");
        }

        $scope.loginClicked = function () {
          $timeout(function () {
            var usernameElem = document.getElementById('login-username');
            angular.element(usernameElem).triggerHandler('input'); // required by automated tests

            var passwordElem = document.getElementById('login-password');
            angular.element(passwordElem).triggerHandler('input'); // required by automated tests

            $scope.password_error = '';

            if (!validate()) {
              return;
            }

            var credentials = {
              email: $scope.email,
              password: $scope.password
            };
            $scope.isLoading = true;
            MessageBus.request(MessageBus.LOGIN, credentials, function () {}, true);
          });
        };

        if (Configurator.useForcedLogin) {
          MessageBus.subscribe(MessageBus.USER_REGISTERED, function (message) {
            if (message) {
              handleUserRegistered(message.params);
            }
          });
          MessageBus.request(MessageBus.USER_REGISTERED, function (message) {
            if (message) {
              handleUserRegistered(message.result);
            }
          });
          MessageBus.subscribe(MessageBus.SERVICE_READY, function (message) {
            MessageBus.request(MessageBus.USER_REGISTERED, function (message) {
              if (message) {
                handleUserRegistered(message.result);
              }
            });
          });
          MessageBus.subscribe(MessageBus.LOGIN, function (data) {
            var result = data.params;
            MessageBus.trace("Login response:" + JSON.stringify(result));

            switch (result.error) {
              case authenticatorError.SUCCESS: // fall through

              case authenticatorError.NO_VALID_SUBSCRIPTIONS:
                MessageBus.request(MessageBus.LICENSE_UPDATE, function () {
                  switchToMainView();
                });
                break;

              case authenticatorError.INVALID_USERNAME_OR_PASSWORD:
                $scope.password_error = gettextCatalog.getString('Invalid credentials');
                break;

              default:
                $scope.password_error = gettextCatalog.getString('Oops. Sorry, there was a error in the authentication process. Try again later or contact Support.');
            }

            $scope.isLoading = false;
          });
        }
      }]
    };
  });
};

},{}],20:[function(require,module,exports){
"use strict";

var isWindowsClient = window.external !== undefined && typeof window.external.SendMessage !== "undefined";
var isMacClient = window.MacAppController !== undefined;
var isUWPClient = window.UWPAppController !== undefined;

module.exports = function (app) {
  app.directive('header', function () {
    return {
      templateUrl: 'views/directives/header.html',
      replace: true,
      scope: {},
      controller: ['$scope', 'MessageBus', 'Telemetry', 'AppHostStorage', 'gettextCatalog', 'oe', 'Features', 'Configurator', 'License', function ($scope, MessageBus, Telemetry, AppHostStorage, gettextCatalog, oe, Features, Configurator, License) {
        $scope.loggedin = true;
        $scope.emailConfirmed = true;
        $scope.licenseType = License.getLicenseType();
        $scope.ProductName = Configurator.getLabels().ProductName;
        $scope.BrandName = Configurator.getLabels().BrandName;
        $scope.SublogoTextPro = Configurator.getLabels().SublogoTextPro;
        $scope.helpMenuVisible = false;
        $scope.accountMenuVisible = false;
        $scope.accountText = "";
        $scope.getProText = Configurator.getStrings().getPro;
        $scope.isSandBoxed = false;
        $scope.storedUserProfile = null;
        $scope.isWindowsClient = isWindowsClient;
        $scope.headerDisabled = Configurator.onlyProConnects && $scope.licenseType !== "Pro";
        $scope.currentView = "mainView";
        $scope.hideAbout = Configurator.hideAboutMenuEntry;

        if (Configurator.useForcedLogin) {
          MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, "forcedLoginView");
        }

        MessageBus.on(MessageBus.VPN_CHANGEVIEW, function (newView) {
          $scope.currentView = newView;
        });

        $scope.accountTextIsHidden = function () {
          return Configurator.hideAccountText && !$scope.loggedin && (!$scope.accountText || $scope.accountText === '');
        };

        $scope.showAccountButton = function () {
          return Configurator.showAccountButton;
        };

        $scope.showHelpButton = function () {
          return Configurator.showHelpButton;
        };

        $scope.toggleHelpMenu = function () {
          $scope.helpMenuVisible = !$scope.helpMenuVisible;
        };

        $scope.hideHelpMenu = function (event, idToIgnore) {
          if (event.target.id !== idToIgnore) $scope.helpMenuVisible = false;
        };

        $scope.toggleAccountMenu = function () {
          $scope.accountMenuVisible = !$scope.accountMenuVisible;
        };

        $scope.hideAccountMenu = function (event, idToIgnore) {
          if (!event.target.parentElement) {
            return;
          }

          if (event.target.id !== idToIgnore && event.target.parentElement.id !== idToIgnore) $scope.accountMenuVisible = false;
        };

        $scope.logout = function () {
          return oe.logout().then(function () {
            MessageBus.request(MessageBus.LOGOUT, function () {
              oe.requestPermanentToken();
              $scope.storedUserProfile = null;
              $scope.accountMenuVisible = false;
              MessageBus.trigger(MessageBus.VPN_USER_REGISTERED, false);
              MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, "mainView");
              Telemetry.sendEvent(Telemetry.LOGOUT);
            });
          });
        };

        $scope.shouldShowLogout = function () {
          return (isMacClient || isUWPClient) && $scope.loggedin && $scope.emailConfirmed && Configurator.allowLogout;
        };

        var userProfileInAppLogin = function userProfileInAppLogin() {
          if (!$scope.loggedin) {
            MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, "registerView");
          } else {
            oe.isEmailConfirmed().then(function (isConfirmed) {
              var emailWasAlreadyConfirmed = $scope.emailConfirmed;
              $scope.emailConfirmed = isConfirmed;

              if ($scope.emailConfirmed) {
                MessageBus.request(MessageBus.OPEN_DASHBOARD);

                if (!emailWasAlreadyConfirmed) {
                  MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, "mainView");
                }
              } else {
                MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, "emailConfirmationView");
              }
            })["catch"](function (error) {
              MessageBus.trace("Header: Failed to check if email is confirmed. Error: ".concat(JSON.stringify(error)));
            });

            if ($scope.accountMenuVisible) {
              $scope.accountMenuVisible = false;
            }
          }
        };

        $scope.userProfile = function () {
          if (Configurator.openDashboardInUI && $scope.loggedin) {
            MessageBus.appHostRequest(MessageBus.OPEN_URL_IN_DEFAULT_BROWSER, {
              url: Configurator.dashboardUrl
            }, function () {});
            return;
          }

          if (isWindowsClient || !Configurator.emailConfirmationNeeded) {
            if (!$scope.loggedin) {
              MessageBus.request(MessageBus.REGISTER_USER);
            } else {
              MessageBus.request(MessageBus.OPEN_DASHBOARD);
            }
          } else {
            userProfileInAppLogin();
          }
        };

        $scope.accountButtonClicked = function () {
          if (!$scope.shouldShowLogout()) {
            $scope.userProfile();
          } else {
            $scope.toggleAccountMenu();
          }
        };

        var getLangIdentifier = function getLangIdentifier() {
          var exclusions = ["pt-br", "zh-cn", "zh-tw"];
          var lang = gettextCatalog.getCurrentLanguage().toLowerCase();
          lang = lang.replace("_", "-");

          if (-1 == exclusions.indexOf(lang)) {
            lang = lang.substr(0, 2);
          }

          return lang;
        };

        $scope.sendFeedbackClicked = function () {
          MessageBus.request("productInfo/get", function (message) {
            var productInfo = message.result;
            var macBaseLink = Configurator.feedbackUrlMac;
            var winBaseLink = Configurator.feedbackUrlWin;
            var surveyLink = productInfo.PlatformType === "OSX" ? macBaseLink : winBaseLink;
            var feedbackUrl = surveyLink + "DeviceID=".concat(productInfo.DeviceId, "&ProductID=").concat(productInfo.ProductID, "&ProductVersion=").concat(productInfo.ProductVersion, "&ProductLanguage=").concat(productInfo.ProductLanguage, "&PlatformType=").concat(productInfo.PlatformType, "&PlatformVersion=").concat(productInfo.PlatformVersion, "&BundleID=").concat(productInfo.BundleID, "&LicenseType=").concat($scope.licenseType);
            MessageBus.appHostRequest(MessageBus.OPEN_URL_IN_DEFAULT_BROWSER, {
              url: feedbackUrl
            }, function () {});
          });
          $scope.helpMenuVisible = false;
        };

        $scope.showSupportPageClicked = function () {
          var lang = getLangIdentifier();
          var supportUrl = Configurator.supportUrl(lang);
          MessageBus.appHostRequest(MessageBus.OPEN_URL_IN_DEFAULT_BROWSER, {
            url: supportUrl
          }, function () {});
          $scope.helpMenuVisible = false;
        };

        var CreateAboutUrl = function CreateAboutUrl() {
          var exclusions = ["pt-br", "zh-cn", "zh-tw"];
          var lang = gettextCatalog.getCurrentLanguage().toLowerCase();
          lang = lang.replace("_", "-");

          if (-1 == exclusions.indexOf(lang)) {
            lang = lang.substr(0, 2);
          }

          return Configurator.aboutUrl(lang);
        };

        $scope.showDiagnosticMenuEntry = function () {
          var features = Features.getFeatures;
          return features && features.diagnosticTool && features.diagnosticTool.enabled;
        };

        $scope.aboutVpnClicked = function () {
          var aboutUrl = CreateAboutUrl();
          MessageBus.appHostRequest(MessageBus.OPEN_URL_IN_DEFAULT_BROWSER, {
            url: aboutUrl
          }, function () {});
          $scope.helpMenuVisible = false;
        };

        $scope.closeClicked = function () {
          MessageBus.appHostRequest("hide", null);
        };

        $scope.showSettingsClicked = function () {
          MessageBus.trigger(MessageBus.VPN_VIEW, "settingsView", "mainView");
        };

        $scope.showStartDiagnosticData = function () {
          MessageBus.trigger(MessageBus.VPN_VIEW, "startDiagnosticDataView", "mainView");
          $scope.helpMenuVisible = false;
        };

        $scope.upgradeButtonClicked = function () {
          Telemetry.sendEvent(Telemetry.UPGRADE_CLICKED, {
            "UI Button": "Header"
          });

          if ($scope.isSandBoxed) {
            MessageBus.trigger(MessageBus.VPN_VIEW, "purchaseView", "mainView");
          } else {
            MessageBus.request(MessageBus.UPGRADE);

            if (isMacClient && $scope.currentView === "waitingWindowView") {
              MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, "mainView");
            }
          }
        };

        var updateAccountText = function updateAccountText() {
          var name = "";

          if ($scope.storedUserProfile && ($scope.storedUserProfile.first_name || $scope.storedUserProfile.last_name)) {
            name = $scope.storedUserProfile.first_name || $scope.storedUserProfile.last_name;
            $scope.accountText = name;
          } else if ($scope.loggedin) {
            $scope.accountText = $scope.emailConfirmed ? gettextCatalog.getString("My Account") : gettextCatalog.getString("Registering...");
          } else {
            $scope.accountText = gettextCatalog.getString("Register");
          }
        }; //used for unit tests


        $scope.updateAccountText = updateAccountText;
        MessageBus.on(MessageBus.VPN_UPDATE_STRINGS, function () {
          $scope.getProText = Configurator.getStrings().getPro;
        });

        var disableHeaderIfEulaNotAccepted = function disableHeaderIfEulaNotAccepted() {
          AppHostStorage.get("eula_accepted").then(function (data) {
            var accepted = false;

            if (data && JSON.stringify(data) !== '{}') {
              accepted = data.accepted;
            }

            if (!accepted) {
              $scope.headerDisabled = true;
            }
          });
        };

        var disableHeaderIfInThemeSelection = function disableHeaderIfInThemeSelection() {
          MessageBus.request(MessageBus.THEME_SELECTION_GET, function (message) {
            MessageBus.trace("Theme selection display status : " + JSON.stringify(message));
            var themeSelection = message.result;

            if (!themeSelection.displayed) {
              $scope.headerDisabled = true;
            }
          });
        };

        MessageBus.on(MessageBus.VPN_IS_SANDBOXED, function (message) {
          $scope.isSandBoxed = message;

          if ($scope.isSandBoxed) {
            disableHeaderIfEulaNotAccepted();
          }
        });
        MessageBus.request(MessageBus.IS_SANDBOXED, function (message) {
          $scope.isSandBoxed = message.result === "True" ? true : false;
          MessageBus.trace("Application is sandboxed: " + $scope.isSandBoxed);

          if ($scope.isSandBoxed) {
            disableHeaderIfEulaNotAccepted();
          }
        });
        MessageBus.request(MessageBus.USER_PROFILE, function (message) {
          $scope.storedUserProfile = message.result;
          updateAccountText();
          MessageBus.trace("Response userProfile: " + JSON.stringify(message.result));
        });
        MessageBus.subscribe(MessageBus.USER_PROFILE_CHANGED, function (message) {
          $scope.storedUserProfile = message.params;
          updateAccountText();
          MessageBus.trace("Notification userProfile changed: " + JSON.stringify(message.params));
        });
        MessageBus.on(MessageBus.VPN_LICENSE_CHANGED, function () {
          var license = License.getLicense();
          MessageBus.trace("License changed " + license.LicenseType);
          $scope.licenseType = license.LicenseType;

          if (Configurator.onlyProConnects && $scope.licenseType === "Pro") {
            $scope.headerDisabled = false;
          }
        });

        var checkIfEmailIsConfirmed = function checkIfEmailIsConfirmed() {
          if ($scope.loggedin === false) {
            return;
          }

          if (isWindowsClient || !Configurator.emailConfirmationNeeded) {
            $scope.emailConfirmed = true;
            return;
          }

          oe.isEmailConfirmed().then(function (isConfirmed) {
            $scope.emailConfirmed = isConfirmed;
            updateAccountText();
          })["catch"](function (error) {
            MessageBus.trace("Purchase Page: Failed to check if email is confirmed. Error: ".concat(JSON.stringify(error)));
          });
        };

        function handleUserRegistered(status) {
          $scope.loggedin = status === "true";
          MessageBus.trace("User login status :" + $scope.loggedin);
          MessageBus.trigger(MessageBus.VPN_USER_REGISTERED, $scope.loggedin);

          if (Configurator.emailConfirmationNeeded) {
            checkIfEmailIsConfirmed();
          }

          updateAccountText();
        }

        MessageBus.subscribe(MessageBus.USER_REGISTERED, function (message) {
          if (message) {
            handleUserRegistered(message.params);
          }
        });
        MessageBus.request(MessageBus.USER_REGISTERED, function (message) {
          if (message) {
            handleUserRegistered(message.result);
          }
        });
        MessageBus.on(MessageBus.VPN_USER_REGISTERED, function (loggedin) {
          $scope.loggedin = loggedin;
          updateAccountText();
        });
        MessageBus.on(MessageBus.VPN_UPDATE_REGISTRATION_STATUS, function (isEmailConfirmed) {
          $scope.emailConfirmed = isEmailConfirmed;
          updateAccountText();
        });
        MessageBus.on(MessageBus.VPN_ENABLE_HEADER, function () {
          $scope.headerDisabled = false;
        });

        if (Configurator.showThemeSelection) {
          disableHeaderIfInThemeSelection();
        }
      }]
    };
  });
};

},{}],21:[function(require,module,exports){
"use strict";

var _index = _interopRequireDefault(require("calculate-size/lib/index.js"));

function _interopRequireDefault(obj) { return obj && obj.__esModule ? obj : { "default": obj }; }

module.exports = function (app) {
  app.directive('keychain', function () {
    return {
      templateUrl: 'views/directives/keychain.html',
      replace: true,
      scope: {},
      controller: ['$scope', 'MessageBus', 'gettextCatalog', 'VpnService', 'AppHostStorage', function ($scope, MessageBus, gettextCatalog, VpnService, AppHostStorage) {
        $scope.alwaysAllowLeft = 0;
        $scope.alwaysAllowTextSize = 14;
        $scope.textDivHeight = 24;
        $scope.allwaysAllowText = gettextCatalog.getString("Always Allow");
        $scope.hideEmptyLine = false;
        $scope.view = "";

        var calculateAlwaysAllowLeft = function calculateAlwaysAllowLeft() {
          var mixPxLeft = 110;
          var maxTextSize = 111;
          var padding = 10;
          var testSize = (0, _index["default"])($scope.allwaysAllowText, {
            font: 'Helvetica',
            fontSize: '14px'
          }).width;

          if (testSize > maxTextSize - padding) {
            $scope.alwaysAllowTextSize = 10;
            $scope.textDivHeight = 20;
            testSize = (0, _index["default"])($scope.allwaysAllowText, {
              font: 'Helvetica',
              fontSize: '10px'
            }).width;
          }

          $scope.alwaysAllowLeft = mixPxLeft + (maxTextSize - padding - testSize) / 2;
        };

        calculateAlwaysAllowLeft();

        $scope.secureConnection = function () {
          var dontShowPageAgain = $('#keychainDontShowId').is(':checked');

          if (dontShowPageAgain) {
            MessageBus.trace("User clicked on Don't show keychain page. Disabling page.");
            AppHostStorage.set("keychain_page_disabled", {
              disabled: true
            });
          }

          VpnService.connectToLastRegion();
          MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, "mainView");
        };

        var checkIfDescriptionFits = function checkIfDescriptionFits() {
          var keychainDecriptionHeight = $('#keychainDescriptionId').height();
          var maxDivHeight = 175;

          if (keychainDecriptionHeight > maxDivHeight) {
            $scope.hideEmptyLine = true;
          }
        };

        MessageBus.on(MessageBus.VPN_CHANGEVIEW, function (newView) {
          $scope.view = newView;
        });
        $scope.$watch('view', function () {
          if ($scope.view == "keychainView") {
            checkIfDescriptionFits();
          }
        });
      }]
    };
  });
};

},{"calculate-size/lib/index.js":1}],22:[function(require,module,exports){
"use strict";

module.exports = function (app) {
  app.directive('bindHtmlUnsafe', function () {
    return function ($scope, $element, $attrs) {
      var compile = function compile(newHTML) {
        // Create re-useable compile function
        //newHTML = $compile(newHTML)($scope); // Compile html
        $element.html('').append(newHTML); // Clear and append it
      };

      var htmlName = $attrs.bindHtmlUnsafe; // Get the name of the variable 
      // Where the HTML is stored

      $scope.$watch(htmlName, function (newHTML) {
        // Watch for changes to 
        // the HTML
        if (!newHTML) return;
        compile(newHTML); // Compile it
      });
    };
  });
  app.directive('location', function () {
    return {
      templateUrl: 'views/directives/location.html',
      replace: true,
      scope: {},
      controller: ['$scope', 'MessageBus', 'RegionList', 'gettext', 'gettextCatalog', 'Telemetry', 'License', 'Configurator', function ($scope, MessageBus, RegionList, gettext, gettextCatalog, Telemetry, License, Configurator) {
        $scope.ok = true;
        $scope.message = "";
        $scope.status = "Disconnected";
        $scope.renewalText = "";
        $scope.renewalButtonText = gettextCatalog.getString("Renew now");
        $scope.licenseType = License.getLicenseType();

        var setConnectingLocation = function setConnectingLocation() {
          $scope.connectingLocation = gettextCatalog.getString('Virtual location: {0}').format(getSelectedRegionName());
        };

        var setPrivacyText = function setPrivacyText(msg) {
          $scope.privacyText = msg;
        };

        MessageBus.on(MessageBus.VPN_RENEW, function (renewData) {
          var daysRemaining = renewData.expiration_days;
          var evaluation = renewData.eval;

          if (daysRemaining < 0) {
            //don't show negative numbers, this should be rare case because the backend should revert to a registered license'
            daysRemaining = 0;
          }

          var msg = evaluation ? gettextCatalog.getString('Enjoy your unlimited data volume.<br/>Remaining days: {0}') : gettextCatalog.getString('Your license will expire soon.<br/>Remaining days: {0}');
          $scope.renewalText = msg.format(daysRemaining);
          $scope.renewalButtonText = evaluation ? gettextCatalog.getString("Get Pro") : gettextCatalog.getString("Renew now");
        });

        var setLocationText = function setLocationText(msg) {
          var linkStart = msg.search("<a>");
          var linkEnd = msg.search("</a>");

          if (linkStart > 0 && linkEnd > 0) {
            $scope.locationText = msg.substring(0, linkStart);
            $scope.infoLink = msg.substring(linkStart + "<a>".length, linkEnd);
          } else {
            $scope.locationText = msg;
            $scope.infoLink = "";
          }
        }; // Used in unit test


        $scope.setLocationText = setLocationText;

        var getSelectedRegionName = function getSelectedRegionName() {
          try {
            return $scope.region ? $scope.region.name : gettext("Unknown");
          } catch (e) {
            return gettext("Unknown");
          }
        };

        var createInfoText = function createInfoText(status) {
          if (Configurator.onlyProConnects && $scope.licenseType !== "Pro") {
            setLocationText("");
            setPrivacyText("");
            return;
          }

          if (status === "Connected") {
            setPrivacyText(gettextCatalog.getString('Your connection is secure.'));
            setLocationText(gettextCatalog.getString('Virtual location set to <a> {0} </a>').format(getSelectedRegionName()));
            $scope.trafficLimitReached = false;
          } else if (status === "Disconnected") {
            setPrivacyText(gettextCatalog.getString('Your connection is unsecure'));

            if ($scope.trafficLimitReached === true) {
              setLocationText(gettextCatalog.getString('Traffic limit reached'));
            } else {
              setLocationText(gettextCatalog.getString('Virtual location set to <a> {0} </a>').format(getSelectedRegionName()));
            }
          } else if (status === "Connecting") {
            setPrivacyText(gettextCatalog.getString('Connecting...'));
            setConnectingLocation();
          } else if (status === "Disconnecting") {
            setPrivacyText(gettextCatalog.getString('Disconnecting...'));
            setConnectingLocation();
          }
        };

        $scope.choseLocation = function () {
          MessageBus.trigger(MessageBus.VPN_VIEW, "regionsView", "mainView");
        };

        MessageBus.on(MessageBus.VPN_STATUS_CHANGED, function (status) {
          $scope.status = status;
          createInfoText(status);
        });
        MessageBus.on(MessageBus.VPN_TRAFFIC_LIMIT_REACHED, function () {
          $scope.trafficLimitReached = true;
          setLocationText(gettextCatalog.getString('Traffic limit reached'));
        });
        MessageBus.on(MessageBus.SELECTED_REGION_CHANGED, function (r) {
          $scope.region = r;
          createInfoText($scope.status);
        });
        MessageBus.on(MessageBus.VPN_LICENSE_CHANGED, function () {
          $scope.licenseType = License.getLicenseType();
          createInfoText($scope.status);
        });
        $scope.regions = RegionList.regions;
        $scope.region = RegionList.selected;
        MessageBus.on(MessageBus.VPN_REMOVE_TRAFFIC_LIMIT, function () {
          $scope.trafficLimitReached = false;
          createInfoText($scope.status);
        });

        $scope.registerAction = function () {
          MessageBus.request(MessageBus.REGISTER_USER);
        };

        $scope.upgradeButtonClicked = function () {
          Telemetry.sendEvent(Telemetry.UPGRADE_CLICKED, {
            "UI Button": "Renew"
          });
          MessageBus.request(MessageBus.UPGRADE);
        };
      }]
    };
  });
};

},{}],23:[function(require,module,exports){
"use strict";

/*! Angular clickout v1.0.2 | � 2014 Greg Berg� | License MIT */
module.exports = function (app) {
  app.directive('noMouseOutline', ['$window', '$parse', function ($window, $parse) {
    return {
      restrict: 'A',
      link: function link(scope, element, attrs) {
        element.on('mousedown', function (event) {
          element.addClass("noOutline");
        });
        element.on('keydown', function (event) {
          element.removeClass("noOutline");
        });
      }
    };
  }]);
};

},{}],24:[function(require,module,exports){
"use strict";

module.exports = function (app) {
  app.directive('privacy', function () {
    return {
      templateUrl: 'views/directives/privacy.html',
      replace: true,
      scope: {},
      controller: ['$scope', 'MessageBus', 'gettextCatalog', 'AppHostStorage', 'oe', function ($scope, MessageBus, gettextCatalog, AppHostStorage, oe) {
        $(".nano").nanoScroller();
        $(".nano-pane").css("display", "block");
        $(".nano-slider").css("display", "block");
        MessageBus.on(MessageBus.VPN_CHANGEVIEW, function (newView) {
          if (newView == "privacyView") {
            setTimeout(function () {
              $('.nano').nanoScroller();
            }, 0);
          }
        });

        $scope.agreeAndContinue = function () {
          AppHostStorage.set("eula_accepted", {
            accepted: true
          });
          oe.sendGdprConsentToConnect();
          MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, "diagnosticsView");
        };

        $scope.openEula = function () {
          var lang = gettextCatalog.getCurrentLanguage().substr(0, 2);
          var url = "https://www.avira.com/".concat(lang, "/end-user-license-agreement-terms-of-use");
          MessageBus.appHostRequest(MessageBus.OPEN_URL_IN_DEFAULT_BROWSER, {
            url: url
          });
        };

        $scope.openTermsAndConditions = function () {
          var lang = gettextCatalog.getCurrentLanguage().substr(0, 2);
          var url = "https://www.avira.com/".concat(lang, "/standard-terms-conditions-business");
          MessageBus.appHostRequest(MessageBus.OPEN_URL_IN_DEFAULT_BROWSER, {
            url: url
          });
        };

        $scope.getPrivacyPolicyLink = function () {
          var lang = gettextCatalog.getCurrentLanguage().substr(0, 2);
          return "https://www.avira.com/".concat(lang, "/general-privacy");
        };

        $scope.openPrivacyAndPolicy = function () {
          MessageBus.appHostRequest(MessageBus.OPEN_URL_IN_DEFAULT_BROWSER, {
            url: $scope.getPrivacyPolicyLink()
          });
        };
      }]
    };
  });
};

},{}],25:[function(require,module,exports){
"use strict";

module.exports = function (app) {
  app.directive('progressDiagnosticData', function () {
    return {
      templateUrl: 'views/directives/progress_diagnostic_data.html',
      replace: true,
      scope: {},
      controller: ['$scope', 'MessageBus', 'gettextCatalog', 'DiagnosticData', function ($scope, MessageBus, gettextCatalog, DiagnosticData) {
        MessageBus.subscribe(MessageBus.DIAGNOSTIC_DATA_STATUS, function (message) {
          try {
            var params = message ? message.params : null;
            $scope.reference = params ? params.id : null;

            if ($scope.reference) {
              DiagnosticData.reference = $scope.reference;
              MessageBus.trigger(MessageBus.VPN_VIEW, "confirmSentDataView", "mainView");
            } else {
              MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, "mainView");
            }
          } catch (error) {
            MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, "mainView");
          }
        });
      }]
    };
  });
};

},{}],26:[function(require,module,exports){
"use strict";

var isMacClient = window.MacAppController !== undefined;

module.exports = function (app) {
  app.directive('pulsar', function () {
    return {
      templateUrl: 'views/directives/pulsar.html',
      replace: true,
      scope: {},
      controller: ['$scope', 'Settings', '$timeout', 'MessageBus', 'gettextCatalog', 'Telemetry', 'License', function ($scope, Settings, $timeout, MessageBus, gettextCatalog, Telemetry, License) {
        $scope.infoRibbonVisible = false;
        $scope.message = gettextCatalog.getString('Your connection is unsecure');
        $scope.infoText = '';
        $scope.trial = false;
        $scope.trialSlideOn = false;
        $scope.trialText = gettextCatalog.getString("Test Pro for free");
        $scope.features = {};
        $scope.trialMaxWidth = 0;
        $scope.pulsarImageClickCount = 0;
        $scope.stopPulsar = false;
        $scope.licenseType = License.getLicenseType();
        var canvas = document.createElement("canvas");

        var getTextWidthInPixels = function getTextWidthInPixels(text, font) {
          var context = canvas.getContext("2d");
          context.font = font;
          var metrics = context.measureText(text);
          return metrics.width;
        };

        var setFoldedSliderText = function setFoldedSliderText() {
          if (isMacClient) {
            $scope.trialText = gettextCatalog.getString("Special Offer");
          } else {
            $scope.trialText = gettextCatalog.getString("Test Pro for free");
          }

          return getTextWidthInPixels($scope.trialText, "16pt arial");
        };

        var setExtendedSliderText = function setExtendedSliderText() {
          $scope.trialText = gettextCatalog.getString("Get 3 Months Free");
          return getTextWidthInPixels($scope.trialText, "16pt arial");
        };

        var showTrialLandingPage = function showTrialLandingPage() {
          return Boolean($scope.features.trial_lp);
        };

        var handleTrial = function handleTrial() {
          Telemetry.sendEvent(Telemetry.TRIAL_BANNER_CLICKED);

          if (showTrialLandingPage()) {
            Settings.data.trialHidden = true;
            MessageBus.request(MessageBus.ACTIVATE_TRIAL);
          } else {
            MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, "trialView");
          }
        };

        setFoldedSliderText();

        $scope.trialSliderClicked = function () {
          if (isMacClient) {
            if ($scope.trialSlideOn) {
              MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, "purchaseView");
            } else {
              setExtendedSliderText();
            }

            $scope.trialSlideOn = true;
          } else {
            handleTrial();
          }
        };

        $scope.trialSliderFocusLost = function () {
          if ($scope.trialSlideOn) {
            $timeout(function () {
              if (!$scope.trialSlideOn) {
                setFoldedSliderText();
              }
            }, 600);
          }

          $scope.trialSlideOn = false;
        };

        $scope.pulsarImageClicked = function ($event) {
          if (!$event.ctrlKey || !$event.altKey) return;

          if ($scope.pulsarImageClickCount === 0) {
            $timeout(function () {
              $scope.pulsarImageClickCount = 0;
            }, 10000);
          }

          $scope.pulsarImageClickCount++;

          if ($scope.pulsarImageClickCount === 6) {
            $scope.pulsarImageClickCount = 0;
            MessageBus.request("toggleInsider", function () {});
          }
        };

        $scope.closeButtonClicked = function () {
          $scope.infoRibbonVisible = false;
        };

        var createDisconnectText = function createDisconnectText(remainingSeconds) {
          if (remainingSeconds === 0) {
            return gettextCatalog.getString('Traffic limit reached');
          }

          var msg = gettextCatalog.getString('You will be disconnected in {0} seconds.');
          return msg.format(remainingSeconds);
        };

        var showInfo = function showInfo(infoMessage) {
          $scope.infoRibbonVisible = true;
          $scope.infoText = gettextCatalog.getString(infoMessage);
        };

        $scope.sliderStyle = function () {
          if ($scope.trialSlideOn) {
            return {
              "max-width": setExtendedSliderText() + 20 + "px"
            };
          }

          return {
            "max-width": setFoldedSliderText() + 20 + "px"
          };
        };

        MessageBus.on(MessageBus.VPN_REQUEST_ERROR, function (errorMessage) {
          showInfo(errorMessage);
        });
        MessageBus.on(MessageBus.VPN_ERROR, function (errorMessage) {
          if (typeof errorMessage !== "undefined" && errorMessage.length > 0) {
            showInfo(errorMessage);
          } else {
            $scope.infoRibbonVisible = false;
          }
        });
        MessageBus.on(MessageBus.VPN_TRAFFIC_LIMIT_REACHED, function () {
          showInfo('Traffic limit reached');
        });
        MessageBus.on("SettingsChanged", function (settings) {
          if (settings.data.trialHidden) $scope.trial = false;
        });
        var trialFeatureEnabled = false;

        var configureTrial = function configureTrial() {
          /*if ($scope.licenseType === "Pro") {
            $scope.trial = false;
            return;
          }

          $scope.trial = trialFeatureEnabled;*/
          $scope.trial = false;
        };

        var setFeatures = function setFeatures(features) {
          MessageBus.trace("Features: " + JSON.stringify(features));
          $scope.features = features;
          $scope.trialText = gettextCatalog.getString("Trial");
          trialFeatureEnabled = Boolean(features.trial);
          configureTrial();
        };

        MessageBus.request(MessageBus.FEATURES, function (message) {
          setFeatures(message.result);
        });
        MessageBus.on(MessageBus.VPN_FEATURES, function (features) {
          setFeatures(features);
        });
        configureTrial();
        MessageBus.on(MessageBus.VPN_LICENSE_CHANGED, function () {
          $scope.licenseType = License.getLicenseType();
          configureTrial();
        });
        MessageBus.subscribe(MessageBus.DISCONNECT_TIMER, function (message) {
          try {
            if (!message.params) {
              return;
            }

            if (message.params.stopCountdown === "true") {
              $scope.infoRibbonVisible = false;
              return;
            }

            var disconnectString = createDisconnectText(message.params.RemainingSeconds);
            showInfo(disconnectString);
          } catch (error) {
            MessageBus.trace("Error on disconnect timer.");
          }
        });
        MessageBus.subscribe(MessageBus.UI_VISIBLE, function (message) {
          if (message.params) {
            $scope.stopPulsar = !message.params.isVisible;
          }
        });
      }]
    };
  });
};

},{}],27:[function(require,module,exports){
"use strict";

var isUWPClient = window.UWPAppController !== undefined;

module.exports = function (app) {
  app.directive('purchase', function () {
    return {
      templateUrl: 'views/directives/purchase.html',
      replace: true,
      scope: {},
      controller: ['$window', '$timeout', '$scope', 'MessageBus', 'Telemetry', 'gettextCatalog', 'oe', 'License', function ($window, $timeout, $scope, MessageBus, Telemetry, gettextCatalog, oe, License) {
        var _this2 = this;

        $scope.userRegistered = false;
        $scope.onlyMacSelected = true;
        $scope.yearlyOffer = {
          id: "",
          price: "",
          trial: false
        };
        $scope.monthlyOffer = {
          id: "",
          text: gettextCatalog.getString("Monthly:"),
          price: "",
          trial: false
        };
        $scope.onlyMacImage = "mac_Grey";
        $scope.allDevicesImage = "all_devices_Black";
        $scope.offerDescription = gettextCatalog.getString("Unlimited traffic");
        $scope.buttonText = gettextCatalog.getString("Buy");
        $scope.buttonDisabled = false;
        $scope.displayError = false;
        $scope.errorMessage = "";
        $scope.buttonStyle = "button";
        $scope.platformSpecificOffer = isUWPClient ? gettextCatalog.getString("On my PC") : gettextCatalog.getString("On my Mac");
        $scope.isUWPClient = isUWPClient;
        $scope.trialActivated = false;
        $scope.isYearlyChecked = true;
        $scope.theme = "LightTheme";
        var productCatalogue = {};

        var initNanoScrollBar = function initNanoScrollBar() {
          $(".nano").nanoScroller();
          $(".nano-pane").css("display", "block");
          $(".nano-slider").css("display", "block");
        };

        initNanoScrollBar();
        $('input').on('change', function () {
          var _this = this;

          var name = $(this).attr('name');

          if (name === "offerRadio") {
            $scope.$apply(function () {
              updateOffer($(_this).val() == '1');
            });
          }
        });
        MessageBus.on(MessageBus.VPN_CHANGEVIEW, function (newView) {
          if (newView == "purchaseView") {
            setTimeout(function () {
              $('.nano').nanoScroller();
            }, 0);
          }
        });
        $scope.$on("theme", function (event, value) {
          $scope.theme = value;
          updateOffer($scope.onlyMacSelected);
        });

        $scope.buy = function () {
          var purchasedProductId = getPurchasedProductId();
          var purchasedProduct = productCatalogue.find(function (prod) {
            return prod.id === purchasedProductId;
          });

          if (purchasedProduct.registrationNeeded && isUWPClient) {
            oe.registerDevice(oe.getDeviceData(), oe.getAppData()).then(function () {
              return oe.sendToken();
            }).then(function () {
              return MessageBus.request(MessageBus.UPGRADE);
            });
            return;
          }

          Telemetry.sendEvent(Telemetry.PURCHASE_STARTED);
          MessageBus.request(MessageBus.PURCHASE, purchasedProductId, function () {});
        };

        $scope.back = function () {
          $scope.displayError = false;
          Telemetry.sendEvent(Telemetry.UPGRADE_POSTPONED);
          MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, "mainView");
        };

        $scope.restore = function () {
          MessageBus.request(MessageBus.RESTORE_PURCHASE, function () {});
        };

        var getPurchasedProductId = function getPurchasedProductId() {
          var period = $('#yearlyPriceId').is(':checked') ? "yearly" : "monthly";

          if (period === "monthly") {
            return $scope.monthlyOffer.id;
          } else {
            return $scope.yearlyOffer.id;
          }
        };

        $scope.hideError = function () {
          $scope.displayError = false;
        };

        var updateOffer = function updateOffer(onlyMac) {
          if (!productCatalogue) {
            return;
          }

          if (onlyMac) {
            $scope.onlyMacSelected = true;
            $scope.onlyMacImage = "mac_Black";
            $scope.allDevicesImage = "all_devices_Grey";
            $scope.offerDescription = gettextCatalog.getString("Unlimited traffic");
          } else {
            $scope.onlyMacSelected = false;
            $scope.onlyMacImage = "mac_Grey";
            $scope.allDevicesImage = "all_devices_Black";
            $scope.offerDescription = gettextCatalog.getString("Unlimited traffic on: Windows, macOS, iOS, Android, Google Chrome");
          }

          if ($scope.theme === "DarkTheme") {
            $scope.onlyMacImage = "mac_Grey";
            $scope.allDevicesImage = "all_devices_Grey";
          }

          var prices = getPrice();
          $scope.yearlyOffer = prices.yearly;
          $scope.monthlyOffer = prices.monthly;
          $scope.isYearlyChecked = true;

          if ($scope.trialActivated && prices.monthly.trial) {
            var offer = gettextCatalog.getString("Three-Month free trial, then only <span>{0}</span> monthly");
            $scope.monthlyOffer.text = offer.format($scope.monthlyOffer.price);
            $scope.isYearlyChecked = false;
          } else {
            $scope.monthlyOffer.text = gettextCatalog.getString("Monthly:");
          }
        };

        var selectActiveOffer = function selectActiveOffer() {
          if ($scope.userRegistered && !$scope.isUWPClient) {
            $("#allDevicesOfferId").prop("checked", true);
            $scope.onlyMacSelected = false;
          } else {
            $("#onlyMacOfferId").prop("checked", true);
            $scope.onlyMacSelected = true;
          }

          updateOffer($scope.onlyMacSelected);
        };

        var getPrice = function getPrice() {
          var prices = {
            monthly: {
              id: "",
              price: "",
              trial: false
            },
            yearly: {
              id: "",
              price: "",
              trial: false
            }
          }; //First init prices with normal prices

          for (var i in productCatalogue) {
            if (productCatalogue[i].registrationNeeded === !$scope.onlyMacSelected) {
              prices["".concat(productCatalogue[i].period)].id = productCatalogue[i].id;
              prices["".concat(productCatalogue[i].period)].price = productCatalogue[i].price;
            }
          } //Check for active trial prices


          if ($scope.trialActivated) {
            for (var i in productCatalogue) {
              if (productCatalogue[i].registrationNeeded === !$scope.onlyMacSelected && productCatalogue[i].trial) {
                prices["".concat(productCatalogue[i].period)].id = productCatalogue[i].id;
                prices["".concat(productCatalogue[i].period)].price = productCatalogue[i].price;
                prices["".concat(productCatalogue[i].period)].trial = true;
              }
            }
          }

          return prices;
        };

        var checkIfEmailIsConfirmed = function checkIfEmailIsConfirmed() {
          return oe.isEmailConfirmed().then(function (isConfirmed) {
            $scope.userRegistered = isConfirmed;
            selectActiveOffer();
          })["catch"](function (error) {
            MessageBus.trace("Purchase Page: Failed to check if email is confirmed. Error: ".concat(JSON.stringify(error)));
          });
        };

        if (License.getLicenseType() === "Registered") {
          checkIfEmailIsConfirmed();
        }

        MessageBus.on(MessageBus.VPN_USER_REGISTERED, function (loggedin) {
          if (loggedin) {
            checkIfEmailIsConfirmed();
          }
        });
        MessageBus.on(MessageBus.VPN_UPDATE_REGISTRATION_STATUS, function (isEmailConfirmed) {
          $scope.userRegistered = isEmailConfirmed;
          selectActiveOffer();
        });
        MessageBus.request(MessageBus.PRODUCT_CATALOGUE, function (message) {
          if (message && message.result) {
            productCatalogue = message.result.productCatalogue;
            MessageBus.trace("Product catalogue: " + JSON.stringify(productCatalogue));
            updateOffer($scope.onlyMacSelected);
          }
        });
        MessageBus.subscribe(MessageBus.PRODUCT_CATALOGUE, function (message) {
          if (message && message.params) {
            productCatalogue = message.params.productCatalogue;
            MessageBus.trace("Product catalogue: " + JSON.stringify(productCatalogue));
            updateOffer($scope.onlyMacSelected);
          }
        });

        var showErrorMessage = function showErrorMessage(reason) {
          $scope.errorMessage = gettextCatalog.getString("Failed to purchase. Please try again later.");

          if (reason === "canceled") {
            $scope.displayError = false;
          } else if (reason === "noTransactions") {
            $scope.errorMessage = gettextCatalog.getString("No purchase found. Nothing to restore.");
            $scope.displayError = true;
          } else {
            $scope.displayError = true;
          }
        };

        MessageBus.subscribe(MessageBus.PURCHASE_STATUS, function (message) {
          if (message && message.params) {
            var status = message.params.status;
            $scope.displayError = false;

            if (status === "purchased" || status === "restored") {
              MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, "mainView");
              $scope.buttonText = gettextCatalog.getString("Buy");
              $scope.buttonDisabled = false;
              $scope.buttonStyle = "button";
            } else if (status === "failed") {
              $scope.buttonText = gettextCatalog.getString("Buy");
              $scope.buttonDisabled = false;
              $scope.buttonStyle = "button";
              showErrorMessage(message.params.reason);
            } else if (status === "purchasing") {
              $scope.buttonText = gettextCatalog.getString("Purchasing...");
              $scope.buttonDisabled = true;
              $scope.buttonStyle = "button__disabled";
            } else if (status === "restoring") {
              $scope.buttonText = gettextCatalog.getString("Restoring...");
              $scope.buttonDisabled = true;
              $scope.buttonStyle = "button__disabled";
            }
          }
        });

        var checkForTrial = function checkForTrial(features) {
          $scope.trialActivated = Boolean(features.trial);
          updateOffer($scope.onlyMacSelected);
        };

        MessageBus.request(MessageBus.FEATURES, function (message) {
          checkForTrial(message.result);
        });
        MessageBus.on(MessageBus.VPN_FEATURES, function (features) {
          checkForTrial(features);
        });

        $scope.subscriptionTerms = function () {
          $(".nano").nanoScroller({
            scroll: 'bottom'
          });
          $(".nano").nanoScroller({
            stop: true
          });
          initNanoScrollBar();
          $scope.$apply(function () {
            updateOffer($(_this2).val() == '1');
          });
        };

        $scope.privacyPolicy = function () {
          var lang = gettextCatalog.getCurrentLanguage().substr(0, 2);
          var url = "https://www.avira.com/".concat(lang, "/general-privacy");
          MessageBus.appHostRequest(MessageBus.OPEN_URL_IN_DEFAULT_BROWSER, {
            url: url
          });
        };

        $scope.termsAndConditions = function () {
          var lang = gettextCatalog.getCurrentLanguage().substr(0, 2);
          var url = "https://www.avira.com/".concat(lang, "/standard-terms-conditions-business");
          MessageBus.appHostRequest(MessageBus.OPEN_URL_IN_DEFAULT_BROWSER, {
            url: url
          });
        };
      }]
    };
  });
};

},{}],28:[function(require,module,exports){
"use strict";

module.exports = function (app) {
  app.directive('regions', function () {
    return {
      templateUrl: 'views/directives/regions.html',
      replace: true,
      scope: {},
      controller: ['$timeout', '$scope', 'RegionList', 'MessageBus', 'Settings', 'VpnService', 'Telemetry', 'Configurator', 'License', function ($timeout, $scope, regionList, messageBus, settings, vpnService, Telemetry, Configurator, License) {
        $scope.opened = false;
        $scope.disconnected = true;
        $scope.selectedId = "";
        $scope.view = "mainView";
        $scope.trafficLimitReached = false;
        $scope.features = {};
        $scope.licenseType = License.getLicenseType();
        $scope.isSandBoxed = false;
        messageBus.on(messageBus.VPN_IS_SANDBOXED, function (message) {
          $scope.isSandBoxed = message;
        });
        messageBus.request(messageBus.IS_SANDBOXED, function (message) {
          $scope.isSandBoxed = message.result === "True" ? true : false;
          MessageBus.trace("Application is sandboxed: " + $scope.isSandBoxed);
        });
        messageBus.on(messageBus.VPN_CHANGEVIEW, function (newView) {
          $scope.view = newView;

          if ($scope.view == "regionsView") {
            $timeout(function () {
              $(".nano").nanoScroller({
                scrollTo: $('#reg-' + $scope.selectedId)
              });
            });

            if (regionList) {
              regionList.updateLatency();
            }
          }
        });

        $scope.showProBadge = function (region) {
          var showForRegion = $scope.features.restrictedProRegions && $scope.features.restrictedProRegions.enabled && region.license_type === 'paid';
          return $scope.licenseType !== "Pro" && showForRegion;
        };

        messageBus.on(messageBus.VPN_LICENSE_CHANGED, function () {
          $scope.licenseType = License.getLicenseType();
        });
        messageBus.request(messageBus.FEATURES, function (message) {
          $scope.features = message.result;
        });
        messageBus.on(messageBus.VPN_FEATURES, function (features) {
          $scope.features = features;
        });

        var calculateNanoSize = function calculateNanoSize() {
          return $('body').height() - 67
          /*settings header*/
          - $('#quickSearchId').height() - $('#header').outerHeight() - 2;
        }; // This is a workaround for the empty region list problem. It seems like refreshing the nana scroller too early
        // causes it to not show the content at all. We refresh it after 5 seconds the first time and after that immediatly.


        $scope.$watch(function () {
          try {
            if ($scope.view != "regionsView") return;
            return calculateNanoSize();
          } catch (error) {
            messageBus.trace("Calculating nanoHeight failed.");
            return 0;
          }
        }, function () {
          if ($scope.view != "regionsView") return;
          var height = calculateNanoSize();
          $('.nano').height(height);
          $timeout(function () {
            $(".nano").nanoScroller();
          }, 1);
        });

        $scope.selectRegion = function (region, $event) {
          $event.preventDefault();
          $event.stopPropagation();
          $scope.opened = false;
          $scope.region = region;
          regionList.select(region);
          return false;
        };

        var findSelectedRegionIdx = function findSelectedRegionIdx() {
          var idx = 0;

          for (i = 0; i < regionList.regions.length; ++i) {
            if (regionList.regions[i].id == $scope.selectedId) {
              idx = i;
              break;
            }
          }

          return idx;
        };

        var keys = {
          KEY_RETURN: 13,
          KEY_ESCAPE: 27,
          KEY_SPACE: 32,
          KEY_LEFT: 37,
          KEY_UP: 38,
          KEY_RIGHT: 39,
          KEY_DOWN: 40
        };

        $scope.onRegionKeyPress = function ($event, region) {
          if ($event.keyCode === keys.KEY_SPACE) {
            $scope.regionSelectionClicked(region);
            $event.preventDefault();
            $event.stopPropagation();
          }

          if ($event.keyCode === keys.KEY_RIGHT) {
            $(".nano").nanoScroller();
          }
        };

        $scope.onRegionKeyUp = function ($event, region) {
          if ($event.keyCode === keys.KEY_RETURN && region.id == $scope.region.id) {
            $scope.regionSelectionClicked(region);
          }
        };

        $scope.connectToRegion = function (region) {
          if (!vpnService.isDisconnected) {
            return;
          }

          updateRegionValues(region);
          messageBus.trigger(messageBus.VPN_CHANGEVIEW, "mainView");

          if (!$scope.trafficLimitReached) {
            vpnService.connect(region, "UI Region View");
          }
        };

        var reconnectToOtherRegion = function reconnectToOtherRegion(region) {
          messageBus.trigger(messageBus.VPN_CHANGEVIEW, "mainView");
          vpnService.disconnect(function () {
            $scope.connectToRegion(region);
          }, "GuiRegionSwitch");
        };

        messageBus.subscribe("latency", function (message) {
          if ($scope.view != "regionsView") {
            //we update the latency only if the view is active
            return;
          }

          var region = message.params;

          if (region.id == $scope.selectedId && region.latency == 0 && region.ipstatus == 0) {
            //the pings are invalid, redo
            regionList.updateLatency();
          }
        });

        $scope.onKeyPressed = function ($event) {
          if ($event.keyCode === keys.KEY_DOWN && !$scope.opened) {
            $scope.toggleDropdown();
          } else if ($event.keyCode === keys.KEY_ESCAPE && $scope.opened) {
            $scope.toggleDropdown();
            $scope.selectedId = regionList.selected ? regionList.selected.id : "";
          } else if ($event.keyCode === keys.KEY_DOWN && $scope.opened) {
            var idx = findSelectedRegionIdx();
            idx = idx === regionList.regions.length - 1 ? 0 : idx + 1;
            $scope.selectedId = regionList.regions[idx].id;
          } else if ($event.keyCode === keys.KEY_UP && $scope.opened) {
            var idx = findSelectedRegionIdx();
            idx = idx === 0 ? regionList.regions.length - 1 : idx - 1;
            $scope.selectedId = regionList.regions[idx].id;
          } else if ($event.keyCode === keys.KEY_RETURN && $scope.opened) {
            var idx = findSelectedRegionIdx();
            var region = regionList.regions[idx];
            $scope.opened = false;
            $scope.region = region;
            regionList.select(region);
          }
        };

        $scope.selectItem = function (region) {
          $scope.selectedId = region.id;
        };

        $scope.toggleDropdown = function () {
          if ($scope.disconnected === false) {
            return false;
          }

          $scope.opened = !$scope.opened;
        };

        $scope.hideDropdown = function (event, idToIgnore) {
          if (event.target.id !== idToIgnore) $scope.opened = false;
        };

        $scope.regionSelectionClicked = function (region) {
          console.log("selected region : " + $scope.selectedId);

          if (!$scope.disconnected && $scope.region.host === region.host) {
            if ($scope.region.id !== region.id) {
              updateRegionValues(region);
            }

            messageBus.trace("Already connected to region: " + region.id + " or host: " + region.host);
            messageBus.trigger(messageBus.VPN_CHANGEVIEW, "mainView");
            return;
          }

          if ($scope.showProBadge(region)) {
            Telemetry.sendEvent(Telemetry.UPGRADE_CLICKED, {
              "UI Button": "Regions"
            });

            if ($scope.isSandBoxed) {
              messageBus.trigger(messageBus.VPN_VIEW, "purchaseView", "regionsView");
            } else {
              messageBus.request(messageBus.UPGRADE);
            }

            return;
          }

          if (Configurator.regionsOnlyNearestFree && $scope.licenseType !== "Pro" && region.id !== "nearest") {
            messageBus.trigger(messageBus.DISPLAY_REGIONS_LOCK);
            return;
          }

          if (!$scope.disconnected) {
            reconnectToOtherRegion(region);
          } else {
            $scope.connectToRegion(region);
          }
        };

        var updateRegionValues = function updateRegionValues(region) {
          regionList.select(region);
          $scope.region = region;
          $scope.selectedId = regionList.selected ? regionList.selected.id : "";
        };

        var updateStatus = function updateStatus(status) {
          $scope.disconnected = false;

          if (status === "Disconnected") {
            $scope.disconnected = true;
          }
        };

        $scope.backButtonClicked = function () {
          messageBus.trigger(messageBus.VPN_VIEW_BACK);
        };

        $scope.query = "";

        $scope.doSearch = function () {
          if ($scope.query) {
            $scope.regions = [];
            $scope.regions.push.apply($scope.regions, $scope.fuzySearch.search($scope.query));
            return;
          }

          $scope.regions = [];
          $scope.regions.push.apply($scope.regions, regionList.regions);
        };

        messageBus.on(messageBus.VPN_TRAFFIC_LIMIT_REACHED, function () {
          $scope.trafficLimitReached = true;
        });
        messageBus.on(messageBus.VPN_REMOVE_TRAFFIC_LIMIT, function () {
          $scope.trafficLimitReached = false;
        });
        messageBus.on(messageBus.VPN_STATUS_CHANGED, function (status) {
          updateStatus(status);
        });
        messageBus.on(messageBus.REGIONS_UPDATED, function (newRegions) {
          if (newRegions) {
            $scope.regions = newRegions.slice();
          }

          initRegionsListSearch(newRegions);

          if ($scope.query) {
            $scope.regions = [];
            $scope.regions.push.apply($scope.regions, $scope.fuzySearch.search($scope.query));
          }
        });

        var initRegionsListSearch = function initRegionsListSearch(regionsList) {
          var options = {
            shouldSort: true,
            threshold: 0.0,
            location: 0,
            distance: 100,
            maxPatternLength: 32,
            minMatchCharLength: 1,
            tokenize: true,
            keys: ["name"]
          };
          $scope.fuzySearch = new Fuse(regionsList, options);
        };

        messageBus.on(messageBus.SELECTED_REGION_CHANGED, function (r) {
          $scope.region = r;
          $scope.selectedId = regionList.selected ? regionList.selected.id : "";
          messageBus.appHostRequest("settings/set", settings.data);
        });

        if (regionList.regions && regionList.regions.length > 0) {
          $scope.regions = regionList.regions.slice();
        }

        $scope.region = regionList.selected;
        $scope.selectedId = regionList.selected ? regionList.selected.id : "";
        initRegionsListSearch(regionList.regions);

        $scope.getSpeedIndictator = function (value) {
          if (!value.latency) {
            return "unknown";
          }

          var latency = parseInt(value.latency, 10);

          if (latency <= 150) {
            return "fast";
          }

          if (latency <= 300) {
            return "medium";
          }

          if (latency > 300) {
            return "slow";
          }
        };
      }]
    };
  });
};

},{}],29:[function(require,module,exports){
"use strict";

module.exports = function (app) {
  app.directive('regionsLock', function () {
    return {
      templateUrl: 'views/directives/regions_lock.html',
      replace: true,
      scope: {},
      controller: ['$scope', 'MessageBus', 'gettextCatalog', function ($scope, MessageBus, gettextCatalog) {
        $scope.frameWidth = 392;
        $scope.frameHeight = 550;
        $scope.marginTop = 100;
        var modal = document.getElementById('regionsLockModalId');
        var container = document.getElementById('guiFrameContainerId');

        function calculateModalContainerSize() {
          $scope.frameWidth = $('body').width();
          $scope.frameHeight = $('body').height();
        }

        calculateModalContainerSize();

        function calculateMarginTop() {
          var modalHeight = 175;
          $scope.marginTop = ($scope.frameHeight - modalHeight) / 3;
        }

        calculateMarginTop();

        function hideModal() {
          modal.className = "Modal is-hidden is-visuallyHidden";
        }

        window.onclick = function (event) {
          if (event.target == modal) {
            hideModal();
          }
        };

        MessageBus.on(MessageBus.DISPLAY_REGIONS_LOCK, function () {
          modal.className = "Modal is-visuallyHidden";
          setTimeout(function () {
            modal.className = "Modal clearfix";
          }, 100);
        });

        $scope.closeClicked = function () {
          hideModal();
        };

        $scope.upgradeButtonClicked = function () {
          MessageBus.request(MessageBus.UPGRADE);
          hideModal();
        };
      }]
    };
  });
};

},{}],30:[function(require,module,exports){
"use strict";

module.exports = function (app) {
  app.directive('register', ['oe', function (oe) {
    return {
      templateUrl: 'views/directives/register.html',
      replace: true,
      scope: {},
      controller: ['$window', '$scope', 'MessageBus', 'AppHostStorage', 'Telemetry', 'gettextCatalog', function ($window, $scope, MessageBus, AppHostStorage, Telemetry, gettextCatalog) {
        $scope.isLogin = false;
        $scope.email_error = '';
        $scope.emailPlaceholder = gettextCatalog.getString('Email address');
        $scope.passwordPlaceholder = gettextCatalog.getString('Password');
        $scope.tfa_error = '';
        $scope.codePlaceholder = gettextCatalog.getString('Code');
        $scope.tfa_button = gettextCatalog.getString('Log in');
        $scope.isTfaEnabled = false;
        $scope.isAccountBlocked = false;
        $scope.loginButtonDisabled = false;
        makeSignupPage();

        $scope.toggle = function () {
          $scope.isAccountBlocked = false;
          $scope.isLogin = !$scope.isLogin;

          if ($scope.isLogin) {
            makeLoginPage();
            Telemetry.sendEvent(Telemetry.VIEW_OPENED, {
              "Id": "loginView"
            });
          } else {
            makeSignupPage();
            Telemetry.sendEvent(Telemetry.VIEW_OPENED, {
              "Id": "registerView"
            });
          }
        };

        MessageBus.on(MessageBus.VPN_CHANGEVIEW, function (newView) {
          if (newView === "registerView" && $scope.isLogin) {
            $scope.isLogin = false;
            makeSignupPage();
          }
        });
        MessageBus.on(MessageBus.VPN_USER_REGISTERED, function (loggedin) {
          if (!loggedin) {
            $scope.isLogin = false;
            $scope.email = '';
            $scope.password = '';
            $scope.isTfaEnabled = false;
            $scope.otf = '';
          }
        });

        function makeLoginPage() {
          $scope.title = gettextCatalog.getString('Log in');
          $scope.question = gettextCatalog.getString('New to Avira?');
          $scope.toggleAction = gettextCatalog.getString('Register');
        }

        function makeSignupPage() {
          $scope.title = gettextCatalog.getString('Register');
          $scope.question = gettextCatalog.getString('Already have an account?');
          $scope.toggleAction = gettextCatalog.getString('Log in');
        }

        $scope.hidePopup = function () {
          $scope.email_error = '';
          $scope.password_error = '';
          $scope.tfa_error = '';
        };

        function validate() {
          $scope.hidePopup();

          if (!$scope.loginForm.email.$valid) {
            $scope.email_error = gettextCatalog.getString('Enter a valid email address first');
            return false;
          }

          if (!$scope.loginForm.password.$valid) {
            $scope.password_error = gettextCatalog.getString('Enter a valid password');
            return false;
          }

          return true;
        }

        function parseLoginError(error) {
          switch (error.error) {
            case 'invalid_credentials':
              return gettextCatalog.getString('Invalid credentials');

            case 'required_captcha':
            case 'retries_exceeded':
              $scope.isAccountBlocked = true;
              return '';

            default:
              return gettextCatalog.getString('Oops. Sorry, there was a error in the authentication process. Try again later or contact Support.');
          }
        }

        function parseSignupError(error) {
          var results = {
            email: '',
            password: ''
          };

          switch (error.status) {
            case '409':
              results.email = gettextCatalog.getString('Oops. Sorry, this email address is already registered with another account.');
              break;

            case '400':
              switch (error.code) {
                case '909':
                case '911':
                  results.password = gettextCatalog.getString('Your password must contain at least 8 characters, one digit, and one uppercase letter.');
                  break;

                default:
                  results.email = gettextCatalog.getString('Oops. Sorry, there was a error in the registration process. Try again later or contact Support.');
                  break;
              }

              break;

            default:
              results.email = gettextCatalog.getString('Oops. Sorry, there was a error in the registration process. Try again later or contact Support.');
              break;
          }

          return results;
        }

        function onLogin() {
          MessageBus.trace("Log in successful!");
          Telemetry.sendEvent(Telemetry.LOGIN_SUCCESSFUL);
          oe.sendToken();
          MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, "mainView");
        }

        function executeLogin() {
          if (!validate()) {
            return Promise.resolve(false);
          }

          MessageBus.trace("Log in started.");
          var user = {
            email: $scope.email,
            password: $scope.password
          };
          return oe.loginFromApp(user, oe.getDeviceData(), oe.getAppData()).then(function () {
            return oe.isEmailConfirmed();
          }).then(function (isConfirmed) {
            MessageBus.trace("Login successful. Email confirmed ".concat(isConfirmed));
            onLogin();

            if (!isConfirmed) {
              showEmailConfirmationPage();
            }

            resolve(true);
          })["catch"](function (error) {
            $scope.$apply(function () {
              MessageBus.trace("Log in failed. Error: ".concat(JSON.stringify(error)));

              if (error.error == 'invalid_otp') {
                $scope.question = gettextCatalog.getString('Verification code');
                $scope.isTfaEnabled = true;
                return;
              }

              $scope.email_error = parseLoginError(error);
              resolve(false);
            });
          });
        }

        function signup() {
          if (!validate()) {
            return Promise.resolve(false);
          }

          MessageBus.trace("Registration started.");
          var user = {
            email: $scope.email,
            password: $scope.password,
            gdpr_consent: new Date().toISOString()
          };
          return oe.registerUserFromApp(user, oe.getDeviceData(), oe.getAppData()).then(function (token) {
            onLogin(token);
            showEmailConfirmationPage();
            resolve(true);
          })["catch"](function (error) {
            MessageBus.trace("Registration failed. ".concat(JSON.stringify(error)));

            if (!Array.isArray(error.errors)) {
              error.errors = [{}]; // default error
            }

            var errors = parseSignupError(error.errors[0]);
            $scope.$apply(function () {
              $scope.email_error = errors.email;
              $scope.password_error = errors.password;
            });
            resolve(false);
          });
        }

        var showEmailConfirmationPage = function showEmailConfirmationPage() {
          MessageBus.trace("Showing email confirmation page.");
          AppHostStorage.set("unconfirmed_email", {
            email: $scope.email
          }).then(function () {
            MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, "emailConfirmationView");
            MessageBus.trigger(MessageBus.VPN_UPDATE_REGISTRATION_STATUS, false);
          });
        };

        var getLangIdentifier = function getLangIdentifier() {
          var exclusions = ["pt-br", "zh-cn", "zh-tw"];
          var lang = gettextCatalog.getCurrentLanguage().toLowerCase();
          lang = lang.replace("_", "-");

          if (-1 == exclusions.indexOf(lang)) {
            lang = lang.substr(0, 2);
          }

          return lang;
        };

        $scope.forgotPassword = function () {
          var lang = getLangIdentifier();
          var link = "https://my.avira.com/" + lang + "/auth/forgot";
          MessageBus.appHostRequest(MessageBus.OPEN_URL_IN_DEFAULT_BROWSER, {
            url: link
          }, function () {});
        };

        $scope.openEula = function () {
          var lang = getLangIdentifier();
          var link = "https://www.avira.com/" + lang + "/end-user-license-agreement-terms-of-use";
          MessageBus.appHostRequest(MessageBus.OPEN_URL_IN_DEFAULT_BROWSER, {
            url: link
          }, function () {});
        };

        $scope.openTermsAndConditions = function () {
          var lang = gettextCatalog.getCurrentLanguage().substr(0, 2);
          var url = "https://www.avira.com/".concat(lang, "/standard-terms-conditions-business");
          MessageBus.appHostRequest(MessageBus.OPEN_URL_IN_DEFAULT_BROWSER, {
            url: url
          });
        };

        $scope.getPrivacyPolicyLink = function () {
          var lang = gettextCatalog.getCurrentLanguage().substr(0, 2);
          return "https://www.avira.com/".concat(lang, "/general-privacy");
        };

        $scope.openPrivacyAndPolicy = function () {
          MessageBus.appHostRequest(MessageBus.OPEN_URL_IN_DEFAULT_BROWSER, {
            url: $scope.getPrivacyPolicyLink()
          });
        };

        $scope.backButtonClicked = function () {
          MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, "mainView");
        };

        $scope.accessConnect = function () {
          $scope.isAccountBlocked = false;
          var lang = getLangIdentifier();
          var link = "https://my.avira.com/" + lang + "/auth/login";
          MessageBus.appHostRequest(MessageBus.OPEN_URL_IN_DEFAULT_BROWSER, {
            url: link
          }, function () {});
        };

        $scope.login = function () {
          if ($scope.loginButtonDisabled) return;
          $scope.loginButtonDisabled = true;
          var promise;

          if ($scope.isTfaEnabled) {
            promise = $scope.loginWithOtp();
          } else if ($scope.isLogin) {
            Telemetry.sendEvent(Telemetry.LOGIN_STARTED);
            promise = executeLogin();
          } else {
            Telemetry.sendEvent(Telemetry.REGISTRATION_STARTED);
            promise = signup();
          }

          promise.then(function () {
            $scope.$apply(function () {
              $scope.loginButtonDisabled = false;
            });
          })["catch"](function () {
            $scope.$apply(function () {
              $scope.loginButtonDisabled = false;
            });
          });
        };

        $scope.loginWithOtp = function () {
          if (!$scope.otf) {
            $scope.tfa_error = gettextCatalog.getString('Fill in this field.');
            return Promise.resolve(false);
          }

          var user = {
            email: $scope.email,
            password: $scope.password
          };
          return oe.loginFromApp(user, oe.getDeviceData(), oe.getAppData(), $scope.otf).then(function (token) {
            onLogin();
            resolve(true);
          })["catch"](function (error) {
            $scope.$apply(function () {
              if (error.error == 'invalid_otp') {
                MessageBus.trace("TFA activated. Waiting for code...");
                $scope.tfa_error = gettextCatalog.getString('Enter a valid verification code.');
              } else {
                MessageBus.trace("Log in failed. ".concat(JSON.stringify(error)));
                $scope.isTfaEnabled = false;
                $scope.email_error = parseLoginError(error);
                console.log($scope.email_error);
              }

              resolve(false);
            });
          });
        };
      }]
    };
  }]);
};

},{}],31:[function(require,module,exports){
"use strict";

module.exports = function (app) {
  app.directive('sentDiagnosticData', function () {
    return {
      templateUrl: 'views/directives/sent_diagnostic_data.html',
      replace: true,
      scope: {},
      controller: ['$scope', 'MessageBus', 'gettextCatalog', 'DiagnosticData', function ($scope, MessageBus, gettextCatalog, DiagnosticData) {
        $scope.collect = function () {
          MessageBus.trigger(MessageBus.VPN_VIEW, "progressDiagnosticDataView", "mainView");
          MessageBus.request(MessageBus.DIAGNOSTIC_DATA_SEND, DiagnosticData.userSelection, function () {});
        };
      }]
    };
  });
};

},{}],32:[function(require,module,exports){
"use strict";

module.exports = function (app) {
  app.directive('settings', function () {
    return {
      templateUrl: 'views/directives/settings.html',
      replace: true,
      scope: {
        title: '@settings'
      },
      controller: ['$scope', 'MessageBus', 'gettextCatalog', function ($scope, MessageBus, gettextCatalog) {
        $scope.backButtonClicked = function () {
          if (isSettingsView()) {
            MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, "mainView");
          } else {
            MessageBus.trigger(MessageBus.VPN_VIEW_BACK);
          }
        };

        $scope.onKeyDown = function ($event) {
          if ($event.keyCode === 13) $scope.backButtonClicked();
        };

        var isSettingsView = function isSettingsView() {
          return $scope.title === gettextCatalog.getString("Settings");
        };

        MessageBus.on(MessageBus.VPN_CHANGEVIEW, function (newView) {
          switch (newView) {
            case "settingsView":
              $scope.title = gettextCatalog.getString("Settings");
              break;

            case "wifiView":
              $scope.title = gettextCatalog.getString("Auto-connect VPN");
              break;

            case "regionsView":
              $scope.title = gettextCatalog.getString("Select virtual location");
              break;

            case "startDiagnosticDataView":
              $scope.title = gettextCatalog.getString("Diagnostics");
              break;

            case "collectDiagnosticDataView":
              $scope.title = gettextCatalog.getString("Technical data");
              break;

            case "sentDiagnosticDataView":
              $scope.title = gettextCatalog.getString("Technical data");
              break;

            case "progressDiagnosticDataView":
              $scope.title = gettextCatalog.getString("Technical data");
              break;

            case "confirmSentDataView":
              $scope.title = gettextCatalog.getString("Confirmation");
              break;

            case "displaySettingsView":
              $scope.title = gettextCatalog.getString("Display settings");
              break;

            default:
              break;
          }
        });
      }]
    };
  });
};

},{}],33:[function(require,module,exports){
"use strict";

module.exports = function (app) {
  app.directive('startDiagnosticData', function () {
    return {
      templateUrl: 'views/directives/start_diagnostic_data.html',
      replace: true,
      scope: {},
      controller: ['$scope', '$timeout', 'MessageBus', function ($scope, $timeout, MessageBus) {
        $scope.lastReportSent = false;
        $scope.showCopyButton = true;
        MessageBus.on(MessageBus.VPN_CHANGEVIEW, function (newView) {
          if (newView == "startDiagnosticDataView") {
            MessageBus.request(MessageBus.DIAGNOSTIC_DATA_LAST_REFERENCE, function (message) {
              try {
                var result = message ? message.result : null;
                $scope.lastReportRef = result ? result.id : null;

                if ($scope.lastReportRef) {
                  $scope.lastReportSent = true;
                  $scope.lastReportDate = result ? new Date(result.date).toLocaleString() : null;
                }
              } catch (error) {}
            });
          }
        });

        $scope.next = function () {
          MessageBus.trigger(MessageBus.VPN_VIEW, "collectDiagnosticDataView", "mainView");
        };

        $scope.copyButtonClicked = function () {
          var copyElement = document.getElementById("referenceNumber");
          var range = document.createRange();
          range.selectNode(copyElement);
          window.getSelection().removeAllRanges();
          window.getSelection().addRange(range);
          document.execCommand('copy');
          window.getSelection().removeAllRanges();
          $scope.showCopyButton = false;
          $timeout(function () {
            $scope.showCopyButton = true;
          }, 3000);
        };
      }]
    };
  });
};

},{}],34:[function(require,module,exports){
"use strict";

var isMacClient = window.MacAppController !== undefined;
var isWindowsClient = window.external !== undefined && typeof window.external.SendMessage !== "undefined";

module.exports = function (app) {
  app.directive('status', function () {
    return {
      templateUrl: 'views/directives/status.html',
      replace: true,
      scope: {},
      controller: ['$scope', 'MessageBus', 'RegionList', 'gettextCatalog', 'VpnService', 'Settings', 'AppHostStorage', 'Telemetry', 'Configurator', 'Features', 'License', function ($scope, MessageBus, regionList, gettextCatalog, vpnService, settings, AppHostStorage, Telemetry, Configurator, Features, License) {
        $scope.region = regionList.selected;
        $scope.ipAddressFeature = Features.getFeatures.ipAddress && Features.getFeatures.ipAddress.enabled;
        $scope.ipAddress = "";
        $scope.trafficLimitReached = false;
        $scope.restrictedConnect = false;
        $scope.licenseType = License.getLicenseType();
        $scope.trafficLimitInterval = License.getTrafficInterval();
        $scope.isSandBoxed = false;

        var showFeedbackOnDisconnect = function showFeedbackOnDisconnect() {
          if (!settings.data["feedbackMessageShown"]) {
            MessageBus.request("traffic/get", function (message) {
              if (message.result.used > 4 * 1024 * 1024) {
                var macBaseLink = "https://www.avira.com";
                var winBaseLink = "https://www.avira.com";
                var surveyLink = productInfo.PlatformType === "OSX" ? macBaseLink : winBaseLink;
                var notification = {
                  title: gettextCatalog.getString("Send feedback"),
                  text: gettextCatalog.getString("Tell us what you think. Your feedback helps us improve."),
                  action: "openUrl",
                  parameter: surveyLink
                };
                MessageBus.appHostRequest("showActionNotification", notification, function () {});
              }

              settings.data["feedbackMessageShown"] = 1;
            });
          }
        };

        MessageBus.on(MessageBus.VPN_RENEW, function (renewData) {
          $scope.ipAddressFeature = false;
        });

        var isMacApp = function isMacApp() {
          return isMacClient && $scope.isSandBoxed;
        };

        var isKeyChainPageDisabled = function isKeyChainPageDisabled() {
          return new Promise(function (resolve) {
            return AppHostStorage.get("keychain_page_disabled").then(function (data) {
              resolve(data && data.disabled);
            });
          });
        };

        MessageBus.on(MessageBus.VPN_FEATURES, function (features) {
          $scope.ipAddressFeature = features.ipAddress && features.ipAddress.enabled;
        });
        MessageBus.subscribe(MessageBus.KEYCHAIN_ACCESS_GRANTED, function (message) {
          var accessGranted = message.params && message.params.granted;

          if (accessGranted) {
            MessageBus.trace("User clicked on Always Allow. Disabling keychain page.");
            vpnService.connect($scope.region);
            return;
          }

          MessageBus.trace("Keychain page is not disable. Showing page...");
          vpnService.setCurrentRegion($scope.region);
          MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, "keychainView");
        });

        var connectVpn = function connectVpn(triggerSource) {
          if (!isMacApp()) {
            vpnService.connect($scope.region, triggerSource);
            return;
          }

          isKeyChainPageDisabled().then(function (disabled) {
            if (disabled) {
              vpnService.connect($scope.region);
              return;
            }

            MessageBus.request(MessageBus.KEYCHAIN_ACCESS_GRANTED, function () {});
          });
        };

        var toggleConnect = function toggleConnect(buttonTriggered, triggerSource) {
          if ($scope.status === "Connected") {
            vpnService.disconnect(function () {}, buttonTriggered ? "GuiDisconnectButton" : "GuiOtherDisconnect");

            if (Configurator.showFeedbackOnDisconnect) {
              showFeedbackOnDisconnect();
            }
          } else if ($scope.status === "Disconnected") {
            if ($scope.region == null) {
              var errorMessage = gettextCatalog.getString("The selected location is invalid. Please check the network connection.");
              MessageBus.trigger(MessageBus.VPN_ERROR, errorMessage);
              MessageBus.appHostRequest("showNotification", {
                message: errorMessage,
                notificationId: "Connect Error"
              });
              return;
            }

            connectVpn(triggerSource);
          } else if ($scope.status === "Connecting") {
            vpnService.cancel();
          }
        };

        $scope.buttonClicked = function () {
          if (Configurator.onlyProConnects && $scope.licenseType !== "Pro") {
            Telemetry.sendEvent(Telemetry.UPGRADE_CLICKED, {
              "UI Button": "Connect"
            });
            MessageBus.appHostRequest(MessageBus.OPEN_URL_IN_DEFAULT_BROWSER, {
              url: Configurator.renewalUrl
            }, function () {});
            return;
          }

          if ($scope.trafficLimitReached === true) {
            Telemetry.sendEvent(Telemetry.UPGRADE_CLICKED, {
              "UI Button": "Connect"
            });

            if ($scope.isSandBoxed) {
              MessageBus.trigger(MessageBus.VPN_VIEW, "purchaseView", "mainView");
            } else {
              MessageBus.request(MessageBus.UPGRADE);
            }
          } else {
            toggleConnect(true, "UI Connect Button");
          }
        };

        $scope.isButtonHidden = function () {
          return $scope.status === "Disconnecting" || $scope.status === "Connecting" && !Features.getFeatures.enableCancelConnecting;
        };

        var setRegisterText = function setRegisterText(msg) {
          var linkStart = msg.search("<a>");
          var linkEnd = msg.search("</a>");

          if (linkStart > 0 && linkEnd > 0) {
            $scope.locationText = msg.substring(0, linkStart);
            $scope.infoLink = msg.substring(linkStart + "<a>".length, linkEnd);
            $scope.infoText = msg.substring(linkEnd + "</a>".length);
          } else {
            $scope.locationText = msg;
            $scope.infoLink = "";
            $scope.infoText = "";
          }
        };

        var notifyAppHost = function notifyAppHost(status) {
          var iconId = status.charAt(0).toLowerCase() + status.slice(1);
          MessageBus.appHostRequest("systray/icon/set", iconId, function () {});
        };

        $scope.registerAction = function () {
          if (isWindowsClient) {
            MessageBus.request(MessageBus.REGISTER_USER);
          } else {
            MessageBus.trigger(MessageBus.VPN_VIEW, "registerView", "mainView");
          }
        };

        var setButtonText = function setButtonText(status) {
          if (Configurator.onlyProConnects && $scope.licenseType !== "Pro") {
            return;
          }

          if ($scope.trafficLimitReached) {
            return;
          }

          if (status === "Connected") {
            $scope.connectButtonText = Configurator.getStrings().disconnect;
          } else if (status === "Disconnected") {
            $scope.connectButtonText = Configurator.getStrings().secureMyConnection;
          } else if (status === "Connecting" && Features.getFeatures.enableCancelConnecting) {
            $scope.connectButtonText = Configurator.getStrings().cancel;
          }
        };

        MessageBus.request(MessageBus.IS_SANDBOXED, function (message) {
          $scope.isSandBoxed = message.result === "True" ? true : false;
        });
        MessageBus.on(MessageBus.VPN_IS_SANDBOXED, function (message) {
          $scope.isSandBoxed = message;
        });

        var updateRestrictedContext = function updateRestrictedContext() {
          if ($scope.licenseType === "Pro") {
            MessageBus.trace("Removing connect restriction. User has a valid subscription.");
            $scope.restrictedConnect = false;
            setButtonText($scope.status);
          } else {
            MessageBus.trace("Enabled connect restriction. User does not have a valid subscription.");
            $scope.restrictedConnect = true;
            $scope.connectButtonText = gettextCatalog.getString('Renew now');
          }
        };

        if (Configurator.onlyProConnects) {
          updateRestrictedContext();
        }

        var getErrorMessage = function getErrorMessage(errorCode) {
          switch (errorCode) {
            case 0:
              return "";

            case 1:
              return gettextCatalog.getString("Cannot resolve host address.");

            case 2:
              return gettextCatalog.getString("No network available.");

            case 3:
              return gettextCatalog.getString("Connection to server lost.");

            case 5:
              return gettextCatalog.getString("Fatal error.");

            case 8:
              return gettextCatalog.getString("Tap Adapter not present or disabled.");

            case 9:
              return gettextCatalog.getString("A new driver has been installed, please restart your computer to complete the installation");

            case 11:
              return gettextCatalog.getString("Failed to connect using UDP protocol. Retrying with TCP protocol.");

            case 12:
              return gettextCatalog.getString("IPSec traffic is blocked. Please contact your network administrator.");

            case 13:
              return gettextCatalog.getString("Failed to establish the VPN connection. Please try again.");

            default:
              return gettextCatalog.getString("Unknown error.");
          }
        };

        MessageBus.subscribe(MessageBus.IP_ADDRESS_REFRESHED, function (message) {
          if (message && message.params) {
            $scope.ipAddress = message.params.ip;
          }
        });

        var updateStatus = function updateStatus(status) {
          MessageBus.trace("VPN status changed " + status);

          if ($scope.status !== status && status !== "Connecting" && status !== "Disconnecting") {
            MessageBus.request(MessageBus.REFRESH_IP_ADDRESS, function () {});
          }

          $scope.status = status;
          MessageBus.trigger(MessageBus.VPN_STATUS_CHANGED, $scope.status);
          setButtonText(status);
          notifyAppHost(status);
        };

        MessageBus.request(MessageBus.STATUS, function (message) {
          //trigger new status
          updateStatus(vpnService.status);
        });
        MessageBus.subscribe(MessageBus.STATUS, function (message) {
          if (message.params.error) {
            var errorMessage = getErrorMessage(message.params.error);
            MessageBus.trace("Displaying error banner. ErrorCode: ".concat(message.params.error, ". ErrorMessage: ").concat(errorMessage));
            MessageBus.trigger(MessageBus.VPN_ERROR, errorMessage);
            MessageBus.appHostRequest("showNotification", {
              message: errorMessage,
              notificationId: "Status Error"
            });
          } else {
            MessageBus.trigger(MessageBus.VPN_ERROR, getErrorMessage(0));
          }

          if (message.params.status) {
            updateStatus(message.params.status);
          }
        });
        MessageBus.subscribe("toggleConnect", function () {
          toggleConnect(false, "Systray Icon");
        });
        MessageBus.subscribe("powerModeResumeConnect", function () {
          connectVpn("Power Mode Resume");
        });
        MessageBus.on(MessageBus.SELECTED_REGION_CHANGED, function (r) {
          $scope.region = r;
        });

        if (regionList.selected) {
          $scope.region = regionList.selected;
        }

        MessageBus.on(MessageBus.VPN_TRAFFIC_LIMIT_REACHED, function () {
          $scope.trafficLimitReached = true;
          $scope.connectButtonText = gettextCatalog.getString('Buy unlimited traffic');
          var registerText = "";

          if ($scope.trafficLimitInterval === "monthly") {
            registerText = gettextCatalog.getString("or get 500 MB if you <a> register </a>");
          }

          setRegisterText(registerText);
        });

        var removeTrafficLimit = function removeTrafficLimit() {
          if ($scope.trafficLimitReached) {
            MessageBus.trace("Removing traffic limit. User registered or bought a license.");
            $scope.trafficLimitReached = false;
            setButtonText($scope.status);
            MessageBus.trigger(MessageBus.VPN_ERROR, "");
            MessageBus.trigger(MessageBus.VPN_REMOVE_TRAFFIC_LIMIT);
          }
        };

        MessageBus.on(MessageBus.VPN_LICENSE_CHANGED, function () {
          var oldLicense = $scope.licenseType;
          $scope.licenseType = License.getLicenseType();
          $scope.trafficLimitInterval = License.getTrafficInterval();

          if (Configurator.onlyProConnects) {
            updateRestrictedContext();
          } else if (oldLicense && oldLicense != $scope.licenseType) {
            removeTrafficLimit();
          }
        });
        MessageBus.on(MessageBus.VPN_USER_REGISTERED, function (loggedin) {
          if (loggedin) {
            removeTrafficLimit();
          }
        });
      }]
    };
  });
};

},{}],35:[function(require,module,exports){
"use strict";

module.exports = function (app) {
  app.directive('switch', function () {
    return {
      replace: true,
      transclude: true,
      onSwitchKeyDown: function onSwitchKeyDown($event) {
        if ($event.keyCode === 13) $event.element.checked = $event.element.checked;
      },
      template: function template(element, attrs) {
        var html = '';
        html += '<span';
        html += ' class="switch' + (attrs["class"] ? ' ' + attrs["class"] : '') + '"';
        html += attrs.ngModel ? ' ng-click="' + attrs.ngDisabled + ' ? ' + attrs.ngModel + ' : ' + attrs.ngModel + '=!' + attrs.ngModel + (attrs.ngChange ? '; ' + attrs.ngChange + '()"' : '"') : '';
        html += ' ng-class="{ checked:' + attrs.ngModel + ', disabled:' + attrs.ngDisabled + ' }"';
        html += ' ng-keydown="onSwitchKeyDown($event)"';
        html += '>';
        html += '<small></small>';
        html += '<input type="checkbox"';
        html += attrs.id ? ' id="' + attrs.id + '"' : '';
        html += attrs.name ? ' name="' + attrs.name + '"' : '';
        html += attrs.ngModel ? ' ng-model="' + attrs.ngModel + '"' : '';
        html += attrs.ngDisabled ? ' ng-disabled="' + attrs.ngDisabled + '"' : '';
        html += ' style="display:none" />';
        html += '<span class="switch-text">';
        /*adding new container for switch text*/

        html += attrs.on ? '<span class="on">' + attrs.on + '</span>' : '';
        /*switch text on value set by user in directive html markup*/

        html += attrs.off ? '<span class="off">' + attrs.off + '</span>' : ' ';
        /*switch text off value set by user in directive html markup*/

        html += '</span>';
        return html;
      }
    };
  });
};

},{}],36:[function(require,module,exports){
"use strict";

module.exports = function (app) {
  app.directive('themeSelection', function () {
    return {
      templateUrl: 'views/directives/theme_selection.html',
      replace: true,
      scope: {},
      controller: ['$scope', 'MessageBus', function ($scope, MessageBus) {
        $scope.defaultTheme = "";
        $scope.selectedTheme = "DarkTheme";
        $scope.$on("theme", function (event, value) {
          $scope.selectedTheme = value;

          if ($scope.defaultTheme === "") {
            $scope.defaultTheme = value;
          }
        });
        $('input').on('change', function () {
          var _this = this;

          var name = $(this).attr('name');

          if (name === "themeRadio") {
            $scope.$apply(function () {
              $scope.selectedTheme = $(_this).val();

              if ($scope.defaultTheme === $scope.selectedTheme) {
                MessageBus.trigger(MessageBus.VPN_CHANGE_THEME, "OsSettings");
              } else {
                MessageBus.trigger(MessageBus.VPN_CHANGE_THEME, $(_this).val());
              }
            });
          }
        });

        var showMainView = function showMainView() {
          var themeSelection = {
            displayed: true
          };
          MessageBus.request(MessageBus.THEME_SELECTION_SET, themeSelection, function () {});
          MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, "mainView");
          MessageBus.trigger(MessageBus.VPN_ENABLE_HEADER);
        };

        $scope.getStarted = function () {
          if ($scope.defaultTheme !== $scope.selectedTheme) {
            MessageBus.trigger(MessageBus.VPN_CHANGE_THEME, $scope.selectedTheme);
          }

          showMainView();
        };

        $scope.changeLater = function () {
          if ($scope.defaultTheme !== $scope.selectedTheme) {
            MessageBus.trigger(MessageBus.VPN_CHANGE_THEME, "OsSettings");
          }

          showMainView();
        };
      }]
    };
  });
};

},{}],37:[function(require,module,exports){
"use strict";

module.exports = function (app) {
  app.directive('traffic', function () {
    return {
      templateUrl: 'views/directives/traffic.html',
      replace: true,
      scope: {},
      controller: ['$scope', 'MessageBus', 'gettext', 'gettextCatalog', 'License', 'Configurator', function ($scope, MessageBus, gettext, gettextCatalog, License, Configurator) {
        var formattedTraffic = {
          used: gettextCatalog.getString('0 bytes'),
          limit: gettextCatalog.getString('0 bytes')
        };
        $scope.used = "";
        $scope.limit = "";
        $scope.loggedin = false;
        $scope.traffic = {
          used: 0,
          limit: 0
        };
        $scope.licenseType = License.getLicenseType();
        $scope.trafficLimitInterval = License.getTrafficInterval();

        function getPercentUsed(traffic) {
          if (traffic.limit === 0) return "";
          var percentUsed = traffic.used * 100 / traffic.limit;
          return Math.round(percentUsed).toString() + gettextCatalog.getString('%');
        }

        function updateTrafficInfo() {
          if (Configurator.onlyProConnects && $scope.licenseType !== "Pro") {
            $scope.used = gettextCatalog.getString('Your subscription expired');
            $scope.limit = gettextCatalog.getString('Renew to get unlimited traffic');
            return;
          }

          if ($scope.traffic.limit !== 0) {
            $scope.used = gettextCatalog.getString('{0} used').format(getPercentUsed($scope.traffic));

            switch ($scope.trafficLimitInterval) {
              case "monthly":
                $scope.limit = gettextCatalog.getString('{0} out of {1} monthly secured traffic').format(formattedTraffic.used, formattedTraffic.limit);
                break;

              case "weekly":
                $scope.limit = gettextCatalog.getString('{0} out of {1} weekly secured traffic').format(formattedTraffic.used, formattedTraffic.limit);
                break;

              case "daily":
                $scope.limit = gettextCatalog.getString('{0} out of {1} daily secured traffic').format(formattedTraffic.used, formattedTraffic.limit);
                break;

              default:
                $scope.limit = gettextCatalog.getString('{0} out of {1} monthly secured traffic').format(formattedTraffic.used, formattedTraffic.limit);
            }
          } else {
            $scope.used = gettextCatalog.getString('Unlimited');
            $scope.limit = gettextCatalog.getString('{0} secured traffic').format(formattedTraffic.used);
          }
        }

        function displayTraffic(result) {
          formattedTraffic.used = formatBytes(+result.used);
          formattedTraffic.limit = formatBytes(+result.limit);
          updateTrafficInfo();
        }

        function formatBytes(bytes) {
          if (bytes === 0) {
            return gettextCatalog.getString('0 bytes');
          }

          var thresh = 1024;

          if (Math.abs(bytes) < thresh) {
            return bytes + ' B';
          }

          var units = ['KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'];
          var u = -1;

          do {
            bytes /= thresh;
            ++u;
          } while (Math.abs(bytes) >= thresh && u < units.length - 1);

          return Number(bytes).toLocaleString(undefined, {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
          }) + ' ' + units[u];
        }

        function requestTraffic() {
          MessageBus.request(MessageBus.GETTRAFFIC, function (message) {
            try {
              $scope.traffic = message.result;

              if (+$scope.traffic.used >= +$scope.traffic.limit && $scope.traffic.limit !== 0) {
                MessageBus.trace("Traffic limit reached. Triggering VPN_TRAFFIC_LIMIT_REACHED.");
                MessageBus.trigger(MessageBus.VPN_TRAFFIC_LIMIT_REACHED);
              }

              displayTraffic($scope.traffic);
            } catch (error) {}
          });
        }

        MessageBus.on(MessageBus.VPN_USER_REGISTERED, function (loggedin) {
          $scope.loggedin = loggedin;
          requestTraffic();
        });
        MessageBus.subscribe(MessageBus.TRAFFIC_LIMIT_REACHED, function () {
          MessageBus.trace("TRAFFIC_LIMIT_REACHED triggered.");
          MessageBus.trigger(MessageBus.VPN_TRAFFIC_LIMIT_REACHED);
        });
        MessageBus.subscribe(MessageBus.TRAFFIC, function (message) {
          try {
            $scope.traffic = message.params;
            displayTraffic($scope.traffic);
          } catch (error) {}
        });
        MessageBus.on(MessageBus.VPN_LICENSE_CHANGED, function () {
          $scope.licenseType = License.getLicenseType();
          $scope.trafficLimitInterval = License.getTrafficInterval();
          updateTrafficInfo();
        });
        requestTraffic();
      }]
    };
  });
};

},{}],38:[function(require,module,exports){
"use strict";

module.exports = function (app) {
  app.directive('trial', function () {
    return {
      templateUrl: 'views/directives/trial.html',
      replace: true,
      scope: {},
      controller: ['$scope', 'Settings', 'MessageBus', 'Telemetry', function ($scope, Settings, MessageBus, Telemetry) {
        $scope.registerClicked = function () {
          Settings.data.trialHidden = true;
          MessageBus.request(MessageBus.ACTIVATE_TRIAL);
          Telemetry.sendEvent(Telemetry.TRIAL_ACTIVATION_STARTED);
          MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, "mainView");
        };

        $scope.notNowClicked = function () {
          Telemetry.sendEvent(Telemetry.TRIAL_ACTIVATION_POSTPONED);
          MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, "mainView");
        };
      }]
    };
  });
};

},{}],39:[function(require,module,exports){
"use strict";

module.exports = function (app) {
  app.directive('waitingWindow', function () {
    return {
      templateUrl: 'views/directives/waiting_window.html',
      replace: true,
      scope: {},
      controller: ['$scope', 'Settings', 'MessageBus', 'gettextCatalog', 'Telemetry', 'VpnService', 'RegionList', 'Features', function ($scope, Settings, MessageBus, gettextCatalog, Telemetry, vpnService, RegionList, Features) {
        var DefaultConnectTimeout = 10;
        $scope.virtualLocationText = "";
        $scope.secondsRemaining = DefaultConnectTimeout;
        MessageBus.on(MessageBus.VPN_CHANGEVIEW, function (newView) {
          if (newView == "waitingWindowView") {
            $scope.virtualLocationText = gettextCatalog.getString('Virtual location: {0}').format(RegionList.selected.name);
            startCounter();
          }
        });
        var counter = null;

        var stopCounter = function stopCounter() {
          if (counter) {
            clearInterval(counter);
            counter = null;
          }
        };

        var startCounter = function startCounter() {
          var features = Features.getFeatures;
          $scope.secondsRemaining = DefaultConnectTimeout;

          if (features.waitingWindow.params && features.waitingWindow.params.connect_timeout) {
            $scope.secondsRemaining = features.waitingWindow.params.connect_timeout;
          }

          counter = setInterval(function () {
            $scope.$apply(function () {
              $scope.secondsRemaining = $scope.secondsRemaining - 1;
            });

            if ($scope.secondsRemaining === 0) {
              Telemetry.sendEvent(Telemetry.WAITING_TIME_EXPIRED);
              vpnService.forceConnect(RegionList.selected);
              MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, "mainView");
              stopCounter();
            }
          }, 1000);
        };

        $scope.getPro = function () {
          stopCounter();
          Telemetry.sendEvent(Telemetry.UPGRADE_CLICKED, {
            "UI Button": "WaitingWindow"
          });
          MessageBus.request(MessageBus.UPGRADE);
          MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, "mainView");
        };

        $scope.cancel = function () {
          Telemetry.sendEvent(Telemetry.WAITING_CANCELED);
          stopCounter();
          MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, "mainView");
        };
      }]
    };
  });
};

},{}],40:[function(require,module,exports){
"use strict";

module.exports = function (app) {
  app.directive('wifi', function () {
    return {
      templateUrl: 'views/directives/wifi.html',
      replace: true,
      scope: {},
      controller: ['$scope', 'MessageBus', function ($scope, MessageBus) {
        $scope.wifis = [];
        MessageBus.on(MessageBus.VPN_CHANGEVIEW, function (newView) {
          $scope.view = newView;
        });

        var calculateNanoSize = function calculateNanoSize() {
          var borderSize = 2; //px

          return $('body').height() - 67
          /*settings header*/
          - borderSize - $('#header').outerHeight();
        }; // This is a workaround for the empty list problem. It seems like refreshing the nano scroller too early
        // causes it to not show the content at all. We refresh it after 5 seconds the first time and after that immediatly.


        $scope.$watch(function () {
          try {
            if ($scope.view != "wifiView") return;
            return calculateNanoSize();
          } catch (error) {
            return 0;
          }
        }, function () {
          if ($scope.view != "wifiView") return;
          $('.nano').height(calculateNanoSize);
          $timeout(function () {
            $(".nano").nanoScroller();
          }, 1);
        });

        var updateWifiList = function updateWifiList(wifis) {
          $scope.wifis.length = 0;

          for (var i = 0; i < wifis.length; ++i) {
            $scope.wifis.push(wifis[i]);
          }
        };

        $scope.deleteWifi = function (wifi) {
          if (wifi.Connected) {
            return;
          }

          MessageBus.trace("[!] delete for " + wifi.Id);
          MessageBus.request(MessageBus.WIFI_DELETE, wifi.Id, function (message) {
            MessageBus.request("wifis/get", function (wifis) {
              if (wifis && wifis.result) {
                updateWifiList(wifis.result);
              }
            });
          });
        };

        $scope.updateWifis = function (wifi) {
          MessageBus.trace("[!] update for " + wifi.Id);
          MessageBus.request(wifi.Autoconnect ? MessageBus.WIFI_UNTRUST : MessageBus.WIFI_TRUST, wifi.Id, function (message) {});
        };

        MessageBus.subscribe("wifis/get", function (wifis) {
          var newWifiList = wifis['params'];
          updateWifiList(newWifiList);
        });
        MessageBus.request("wifis/get", function (wifis) {
          var newWifiList = wifis['result'];
          updateWifiList(newWifiList);
        });
      }]
    };
  });
};

},{}],41:[function(require,module,exports){
"use strict";

var IEVersion = function () {
  var undef;
  var v = 3;
  var div = document.createElement('div');
  var all = div.getElementsByTagName('i');

  while (div.innerHTML = '<!--[if gt IE ' + ++v + ']><i></i><![endif]-->', all[0]) {
    ;
  }

  return v > 4 ? v : undef;
}();

var launcherApp = angular.module('LauncherApp', ['gettext', 'rate5Stars']);
launcherApp.config(['$compileProvider', function ($compileProvider) {
  $compileProvider.imgSrcSanitizationWhitelist(/^\s*(https?|ftp|file|chrome-extension|ms-appx-web|ms-appx):|data:image\//);
  $compileProvider.aHrefSanitizationWhitelist(/^\s*(https?|ftp|mailto|file|chrome-extension|ms-appx-web|ms-appx):/);
}]);

var config = require('config');

require('services/messagebus')(launcherApp);

require('services/exceptionHandler')(launcherApp);

require('services/settings')(launcherApp);

require('services/appHostStorage')(launcherApp);

require('services/oe')(launcherApp);

require('services/contextMenu')(launcherApp);

require('services/telemetry')(launcherApp);

require('services/features')(launcherApp);

require('services/license')(launcherApp);

require('services/diagnosticData')(launcherApp);

var getConfigurator = function getConfigurator() {
  switch (config.label) {
    case 'phantom':
      return require('../whitelabel/phantom/scripts/services/configurator');

    case 'invincibull':
      return require('../whitelabel/invincibull/scripts/services/configurator');

    case 'apollo':
      return require('../whitelabel/apollo/scripts/services/configurator');
  }
};

getConfigurator()(launcherApp);

require('directives/bindHtmlUnsafe')(launcherApp);

require('directives/header')(launcherApp);

require('directives/settings')(launcherApp);

require('directives/pulsar')(launcherApp);

require('directives/traffic')(launcherApp);

require('directives/location')(launcherApp);

require('directives/status')(launcherApp);

require('directives/regions')(launcherApp);

require('directives/switch')(launcherApp);

require('directives/clickOut')(launcherApp);

require('directives/trial')(launcherApp);

require('directives/register')(launcherApp);

require('directives/purchase')(launcherApp);

require('directives/keychain')(launcherApp);

require('directives/noMouseOutline')(launcherApp);

require('directives/wifi')(launcherApp);

require('directives/privacy')(launcherApp);

require('directives/diagnostics')(launcherApp);

require('directives/email_confirmation')(launcherApp);

require('directives/waiting_window')(launcherApp);

require('directives/features')(launcherApp);

require('directives/data_usage_popup')(launcherApp);

require('directives/start_diagnostic_data')(launcherApp);

require('directives/collect_diagnostic_data')(launcherApp);

require('directives/confirm_sent_data')(launcherApp);

require('directives/sent_diagnostic_data')(launcherApp);

require('directives/progress_diagnostic_data')(launcherApp);

require('directives/regionsLock')(launcherApp);

require('directives/display_settings')(launcherApp);

require('directives/theme_selection')(launcherApp);

require('directives/forced_login')(launcherApp);

var isMacClient = window.MacAppController !== undefined;
var isUWPClient = window.UWPAppController !== undefined;
var isWindowsClient = window.external !== undefined && typeof window.external.SendMessage !== "undefined";
var Rate5StarsState = {
  DEFAULT: 1,
  FAST_FEEDBACK: 2
};
launcherApp.controller('VpnController', ['$timeout', '$scope', 'MessageBus', 'Settings', 'gettextCatalog', 'Menu', 'gettextFallbackLanguage', 'RegionList', 'VpnService', 'AppHostStorage', 'Telemetry', 'Configurator', 'License', function ($timeout, $scope, MessageBus, Settings, gettextCatalog, menu, gettextFallbackLanguage, regionList, vpnService, AppHostStorage, Telemetry, Configurator, License) {
  $scope.status = "Disconnected";
  $scope.currentView = "mainView";
  $scope.trafficLimitReached = false;
  $scope.licenseType = License.getLicenseType();
  $scope.license = License.getLicense();
  $scope.loggedin = $scope.licenseType !== "Unregistered";
  $scope.isSandBoxed = false;
  $scope.licenseExpirationDate = new Date("01 01 3000");
  $scope.daysUntilExpiration = 0;
  $scope.subscriptionActivated = false;
  $scope.region = regionList.selected;
  $scope.insider = false;
  $scope.showRateDialog = false;
  $scope.useDarkTheme = Configurator.forceDarkTheme;
  $scope.useLightTheme = !$scope.useDarkTheme;
  $scope.showUI = !Configurator.showThemeSelection;
  $scope.$on("theme", function (event, value) {
    if (value === "LightTheme") {
      $scope.useLightTheme = true;
      $scope.useDarkTheme = false;
    } else if (value === "DarkTheme") {
      $scope.useLightTheme = false;
      $scope.useDarkTheme = true;
    }

    $scope.showUI = true;
  });
  var appSettings = {
    appImprovement: false,
    killSwitch: false,
    udpSupport: false,
    autoSecureUntrustedWifi: false,
    malwareProtection: false,
    adBlocking: false,
    showFastFeedback: false
  };
  var defaultLanguageLocale = new Map([['de', 'de_DE'], ['en', 'en_US'], ['es', 'es_ES'], ['fr', 'fr_FR'], ['it', 'it_IT'], ['ja', 'ja_JP'], ['nl', 'nl_NL'], ['pt', 'pt_BR'], ['ru', 'ru_RU'], ['tr', 'tr_TR'], ['zh', 'zh_CN']]);

  var getAppType = function getAppType() {
    if (isUWPClient) {
      $scope.appType = "uwp";
    } else if (isWindowsClient) {
      $scope.appType = "win";
    } else if (isMacClient) {
      return new Promise(function (resolve) {
        MessageBus.request(MessageBus.IS_SANDBOXED, function (message) {
          var sandboxed = message.result === "True" ? true : false;
          $scope.appType = sandboxed === true ? "macApp" : "macDesktop";
        });
      });
    } else {
      $scope.appType = "simulator";
    }
  };

  getAppType();

  window.onerror = function (message) {
    MessageBus.trace("JSException: " + message);
  };

  var isAvailable = function isAvailable(language) {
    var fallbackLanguage = gettextFallbackLanguage(language);
    var testString = 'Quit';
    var dummy = gettextCatalog.getString('Quit'); // just make sure 'Quit' is in the po files.

    return gettextCatalog.getStringFormFor(language, testString, 1) || gettextCatalog.getStringFormFor(fallbackLanguage, testString, 1);
  };

  var initRate5StarsStrings = function initRate5StarsStrings() {
    $scope.rateDefaultTitleText = gettextCatalog.getString('Rate Avira Phantom VPN');
    $scope.rateDefaultDescriptionText = gettextCatalog.getString('Your internet connection was protected from prying eyes. Is this worth 5 stars?');
    $scope.rateDefaultButtonText = gettextCatalog.getString('Rate 5 stars');
    $scope.rateDefaultNotNowButtonText = gettextCatalog.getString('Not now');
    $scope.rateDefaultDontShowAgainText = gettextCatalog.getString("Don't show again");
  };

  var getDefaultLocale = function getDefaultLocale(language) {
    var separatorPos = language.indexOf("_");

    if (-1 != separatorPos) {
      var languageWithoutLocale = language.substr(0, separatorPos);

      if (defaultLanguageLocale.has(languageWithoutLocale)) {
        return defaultLanguageLocale.get(languageWithoutLocale);
      }
    }

    return "en_US";
  };

  var getAppHostTranslatedStrings = function getAppHostTranslatedStrings() {
    var appHostTranslatedStrings = {
      "early_bird_notif_title": gettextCatalog.getString("Avira Phantom VPN Pro released!"),
      "early_bird_notif_message": gettextCatalog.getString("Get for this occasion your three-month free trial. Thank you very much for helping us in beta stage by using and testing our product.")
    };
    return appHostTranslatedStrings;
  };

  MessageBus.appHostRequest("uiLanguage/get", null, function (message) {
    var language = message.result ? message.result.replace("-", "_") : "en-US";

    if (!isAvailable(language)) {
      language = getDefaultLocale(language);
    }

    gettextCatalog.setCurrentLanguage(language);
    menu.initialize();
    var appHostTranslatedStrings = getAppHostTranslatedStrings();
    MessageBus.request(MessageBus.TRANSLATED_STRINGS, appHostTranslatedStrings, function () {});
    initRate5StarsStrings();
    MessageBus.trigger(MessageBus.VPN_UPDATE_STRINGS);
  });

  if (IEVersion) {
    window.document.body.className += ' ie' + IEVersion;
  }

  if (MessageBus.EMBEDDED || isMacClient) {
    window.document.body.className += ' embedded';
  } else {
    window.document.body.className += ' notembedded';
  }

  $scope.isMacClient = function () {
    return isMacClient;
  };

  MessageBus.trace("VPN GUI Application is starting...");
  MessageBus.appHostRequest("startSettings", null, function (message) {
    if (message.result) {
      MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, "settingsView");
    }
  });

  var timeDifference = function timeDifference(dateA, dateB) {
    var msPerDay = 1000 * 60 * 60 * 24;
    var utc1 = Date.UTC(dateA.getFullYear(), dateA.getMonth(), dateA.getDate());
    var utc2 = Date.UTC(dateB.getFullYear(), dateB.getMonth(), dateB.getDate());
    return Math.floor((utc2 - utc1) / msPerDay);
  };

  var setLicenseRenewalText = function setLicenseRenewalText() {
    if ($scope.subscriptionActivated || $scope.daysUntilExpiration > 30) {
      return;
    }

    MessageBus.trigger(MessageBus.VPN_RENEW, {
      eval: $scope.license.eval,
      expiration_days: $scope.daysUntilExpiration
    });
  };

  var updateLicenseRenewal = function updateLicenseRenewal(license) {
    $scope.subscriptionActivated = license.subscription;
    $scope.license = license;
    var parsedDate = Date.parse(license.expiration_date);

    if (parsedDate) {
      $scope.licenseExpirationDate = new Date(parsedDate);

      if ($scope.licenseExpirationDate.getFullYear() == 1) {
        //this is the min value which means unlimited, we just reset it tho the maximum date value.
        $scope.licenseExpirationDate = new Date(8640000000000000);
      }

      $scope.daysUntilExpiration = timeDifference(new Date(Date.now()), $scope.licenseExpirationDate);
      setLicenseRenewalText();
    }
  };

  var showThemeSelectionIfNeeded = function showThemeSelectionIfNeeded() {
    if (Configurator.showThemeSelection) {
      MessageBus.request(MessageBus.THEME_SELECTION_GET, function (message) {
        MessageBus.trace("Theme selection display status : " + JSON.stringify(message));
        var themeSelection = message.result;

        if (themeSelection.displayed) {
          MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, $scope.currentView);
          return;
        }

        MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, "themeSelectionView");
      });
    }
  };

  var checkIfEulaShouldBeShown = function checkIfEulaShouldBeShown() {
    AppHostStorage.get("eula_accepted").then(function (data) {
      if (data && JSON.stringify(data) !== '{}') {
        if (data.accepted === true) {
          MessageBus.request(MessageBus.START_DEVICE_PINGER, function () {});
          showThemeSelectionIfNeeded();
          return;
        }
      }

      MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, "privacyView");
    });
  };

  MessageBus.request("insider/get", function (message) {
    $scope.insider = message.result;
  });

  var requestFastFeedbackStrings = function requestFastFeedbackStrings() {
    MessageBus.request(MessageBus.FAST_FEEDBACK_STRINGS, function (message) {
      $scope.fastFeedbackStrings = message.result;
    });
  };

  var requestFeatures = function requestFeatures() {
    MessageBus.request(MessageBus.FEATURES, function (message) {
      $scope.features = message.result;
      requestFastFeedbackStrings();
      MessageBus.trigger(MessageBus.VPN_FEATURES, $scope.features);
    });
  };

  MessageBus.subscribe(MessageBus.FEATURES, function (message) {
    $scope.features = message.params;
    requestFastFeedbackStrings();
    MessageBus.trigger(MessageBus.VPN_FEATURES, $scope.features);
    MessageBus.request("insider/get", function (message) {
      $scope.insider = message.result;
    });
  });
  requestFeatures();

  var requestAppSettings = function requestAppSettings() {
    MessageBus.request(MessageBus.APPSETTINGSGET, function (message) {
      appSettings = message.result;
    });
  };

  MessageBus.subscribe(MessageBus.APPSETTINGS, function () {
    requestAppSettings();
  });
  requestAppSettings();
  MessageBus.request(MessageBus.IS_SANDBOXED, function (message) {
    $scope.isSandBoxed = message.result === "True" ? true : false;
    MessageBus.trace("Application is sandboxed: " + $scope.isSandBoxed);
    MessageBus.trigger(MessageBus.VPN_IS_SANDBOXED, $scope.isSandBoxed);

    if ($scope.isSandBoxed) {
      checkIfEulaShouldBeShown();
    } else {
      if (isMacClient) {
        MessageBus.request(MessageBus.START_DEVICE_PINGER, function () {});
      }

      showThemeSelectionIfNeeded();
    }
  });
  $scope.status = vpnService.status;

  $scope.onSwitchKeyDown = function ($event) {
    if ($event.keyCode === 13 || $event.keyCode === 32) {
      var checkBox = $event.srcElement.getElementsByTagName("input")[0];
      $timeout(function () {
        checkBox.checked = !checkBox.checked;
        var ngModel = checkBox.getAttribute("ng-model");

        if (ngModel) {
          var path = ngModel.split('.');
          var model = $scope;

          for (i = 0; i < path.length - 1; i++) {
            model = model[path[i]];
          }

          model[path[path.length - 1]] = !model[path[path.length - 1]];
        }
      }, 0);
    }
  };

  MessageBus.on(MessageBus.VPN_STATUS_CHANGED, function (status) {
    $scope.status = status;
  });
  MessageBus.on(MessageBus.VPN_LICENSE_CHANGED, function () {
    $scope.licenseType = License.getLicenseType();
    updateLicenseRenewal(License.getLicense());
  });
  MessageBus.subscribe("openView", function (message) {
    if (message && message.params) {
      MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, message.params);
    }
  });
  MessageBus.on(MessageBus.VPN_CHANGEVIEW, function (newView) {
    if ($scope.currentView != newView) {
      Telemetry.sendEvent(Telemetry.VIEW_OPENED, {
        "Id": newView
      });
    }

    $scope.currentView = newView;
  });
  MessageBus.on(MessageBus.VPN_VIEW, function (newView, oldView) {
    MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, newView);
    $scope.back = oldView;
  });
  MessageBus.on(MessageBus.VPN_VIEW_BACK, function () {
    MessageBus.trigger(MessageBus.VPN_CHANGEVIEW, $scope.back);
  });
  MessageBus.on(MessageBus.VPN_TRAFFIC_LIMIT_REACHED, function () {
    $scope.trafficLimitReached = true;
  });
  MessageBus.on(MessageBus.VPN_REMOVE_TRAFFIC_LIMIT, function (loggedin) {
    $scope.trafficLimitReached = false;
  });
  MessageBus.on(MessageBus.VPN_USER_REGISTERED, function (loggedin) {
    $scope.loggedin = loggedin;
  });
  MessageBus.on("vpn:" + MessageBus.CLOSE, function () {
    MessageBus.appHostRequest("settings/set", Settings.data);
    MessageBus.unsubscribeAll();
    MessageBus.request("exit", null);
  });
  MessageBus.on(MessageBus.SELECTED_REGION_CHANGED, function (r) {
    $scope.region = r;
  });
  Telemetry.sendEvent(Telemetry.GUI_OPENED);

  var setDontDisplayFastFeedback = function setDontDisplayFastFeedback() {
    appSettings.showFastFeedback = false;
    MessageBus.request(MessageBus.APPSETTINGSSET, appSettings, function (message) {});
  };

  $scope.onRateHandler = function (rating) {
    if ($scope.rateState == Rate5StarsState.DEFAULT) {
      MessageBus.request(MessageBus.OPEN_APP_STORE, function () {});
    } else if ($scope.rateState == Rate5StarsState.FAST_FEEDBACK && rating > 0) {
      MessageBus.request(MessageBus.SEND_FAST_FEEDBACK, rating, function (message) {});
    }
  };

  $scope.onNotNowHandler = function () {
    if ($scope.rateState == Rate5StarsState.FAST_FEEDBACK) {
      MessageBus.request(MessageBus.NOT_NOW_FAST_FEEDBACK, function () {});
    }
  };

  $scope.onDontShowAgainHandler = function () {
    if ($scope.rateState == Rate5StarsState.FAST_FEEDBACK) {
      Telemetry.sendEvent(Telemetry.USER_FEEDBACK_HIDDEN);
      setDontDisplayFastFeedback();
    }
  };

  var displayRateMeDialog = function displayRateMeDialog() {
    $scope.rateState = Rate5StarsState.DEFAULT;
    $scope.rateTitleText = $scope.rateDefaultTitleText;
    $scope.rateDescriptionText = $scope.rateDefaultDescriptionText;
    $scope.rateButtonText = $scope.rateDefaultButtonText;
    $scope.notNowButtonText = $scope.rateDefaultNotNowButtonText;
    $scope.rateSelectableStars = false;
    $scope.rateShowDontShowAgain = false;
    $scope.rateAlignButtonsHorizontally = false;
    $scope.showRateDialog = true;
  };

  MessageBus.subscribe(MessageBus.DISPLAY_RATE_ME, function () {
    displayRateMeDialog();
  });

  var displayFastFeedbackDialog = function displayFastFeedbackDialog() {
    $scope.rateState = Rate5StarsState.FAST_FEEDBACK;
    $scope.rateTitleText = $scope.fastFeedbackStrings.title;
    $scope.rateDescriptionText = $scope.fastFeedbackStrings.description;
    $scope.rateButtonText = $scope.fastFeedbackStrings.button_submit;
    $scope.notNowButtonText = $scope.fastFeedbackStrings.button_cancel;
    $scope.rateSelectableStars = true;
    $scope.rateOneStarText = $scope.fastFeedbackStrings.ratings.one;
    $scope.rateTwoStarsText = $scope.fastFeedbackStrings.ratings.two;
    $scope.rateThreeStarsText = $scope.fastFeedbackStrings.ratings.three;
    $scope.rateFourStarsText = $scope.fastFeedbackStrings.ratings.four;
    $scope.rateFiveStarsText = $scope.fastFeedbackStrings.ratings.five;
    $scope.rateShowDontShowAgain = true;
    $scope.rateDontShowAgainText = $scope.rateDefaultDontShowAgainText;
    $scope.rateAlignButtonsHorizontally = true;
    $scope.showRateDialog = true;
  };

  MessageBus.subscribe(MessageBus.DISPLAY_FAST_FEEDBACK, function () {
    displayFastFeedbackDialog();
  });
}]);

},{"../whitelabel/apollo/scripts/services/configurator":71,"../whitelabel/invincibull/scripts/services/configurator":72,"../whitelabel/phantom/scripts/services/configurator":73,"config":9,"directives/bindHtmlUnsafe":10,"directives/clickOut":11,"directives/collect_diagnostic_data":12,"directives/confirm_sent_data":13,"directives/data_usage_popup":14,"directives/diagnostics":15,"directives/display_settings":16,"directives/email_confirmation":17,"directives/features":18,"directives/forced_login":19,"directives/header":20,"directives/keychain":21,"directives/location":22,"directives/noMouseOutline":23,"directives/privacy":24,"directives/progress_diagnostic_data":25,"directives/pulsar":26,"directives/purchase":27,"directives/regions":28,"directives/regionsLock":29,"directives/register":30,"directives/sent_diagnostic_data":31,"directives/settings":32,"directives/start_diagnostic_data":33,"directives/status":34,"directives/switch":35,"directives/theme_selection":36,"directives/traffic":37,"directives/trial":38,"directives/waiting_window":39,"directives/wifi":40,"services/appHostStorage":43,"services/contextMenu":44,"services/diagnosticData":46,"services/exceptionHandler":47,"services/features":48,"services/license":49,"services/messagebus":50,"services/oe":51,"services/settings":53,"services/telemetry":54}],42:[function(require,module,exports){
"use strict";

String.prototype.format = function () {
  var content = this;

  for (var i = 0; i < arguments.length; i++) {
    var replacement = '{' + i + '}';
    content = content.replace(replacement, arguments[i]);
  }

  return content;
};

},{}],43:[function(require,module,exports){
"use strict";

function _classCallCheck(instance, Constructor) { if (!(instance instanceof Constructor)) { throw new TypeError("Cannot call a class as a function"); } }

function _defineProperties(target, props) { for (var i = 0; i < props.length; i++) { var descriptor = props[i]; descriptor.enumerable = descriptor.enumerable || false; descriptor.configurable = true; if ("value" in descriptor) descriptor.writable = true; Object.defineProperty(target, descriptor.key, descriptor); } }

function _createClass(Constructor, protoProps, staticProps) { if (protoProps) _defineProperties(Constructor.prototype, protoProps); if (staticProps) _defineProperties(Constructor, staticProps); return Constructor; }

module.exports = function (module) {
  module.factory('AppHostStorage', ['MessageBus', function (MessageBus) {
    var AppHostStorage = /*#__PURE__*/function () {
      function AppHostStorage(messageBus) {
        _classCallCheck(this, AppHostStorage);

        this.messageBus = messageBus;
      }

      _createClass(AppHostStorage, [{
        key: "set",
        value: function set(name, value) {
          var self = this;
          return new Promise(function (resolve) {
            self.messageBus.request(self.messageBus.STORAGE_SET, {
              key: name,
              value: value
            }, function () {
              resolve();
            });
          });
        }
      }, {
        key: "get",
        value: function get(name) {
          var self = this;
          return new Promise(function (resolve, reject) {
            self.messageBus.request(self.messageBus.STORAGE_GET, name, function (message) {
              if (message && message.result) {
                resolve(message.result);
              } else {
                reject();
              }
            });
          });
        }
      }, {
        key: "remove",
        value: function remove(name) {
          var self = this;
          return new Promise(function (resolve) {
            self.messageBus.request(self.messageBus.STORAGE_REMOVE, name, function () {
              resolve();
            });
          });
        }
      }]);

      return AppHostStorage;
    }();

    return new AppHostStorage(MessageBus);
  }]);
};

},{}],44:[function(require,module,exports){
"use strict";

module.exports = function (module) {
  module.factory('Menu', ['MessageBus', 'gettextCatalog', 'Configurator', function (messageBus, gettextCatalog, Configurator) {
    var menu = {
      initialize: function initialize() {
        messageBus.appHostRequest("menu/add", {
          name: "toggleConnect",
          index: 0,
          text: gettextCatalog.getString("Connect")
        });
        messageBus.appHostRequest("menu/add", {
          name: "menuExit",
          index: 1,
          text: gettextCatalog.getString("Exit")
        });
      }
    };
    messageBus.subscribe("menuExit", function () {
      messageBus.appHostRequest("closeWindow");
    });
    messageBus.on(messageBus.VPN_STATUS_CHANGED, function (status) {
      var PRODUCT_NAME = Configurator.getLabels().ContextMenuName;

      switch (status) {
        case "Connected":
          messageBus.appHostRequest("menu/set", {
            name: "toggleConnect",
            enabled: true,
            text: gettextCatalog.getString("Disconnect")
          });
          messageBus.appHostRequest("systray/tooltip/set", PRODUCT_NAME + " " + gettextCatalog.getString("Your connection is secure"));
          break;

        case "Disconnected":
          messageBus.appHostRequest("menu/set", {
            name: "toggleConnect",
            enabled: true,
            text: gettextCatalog.getString("Connect")
          });
          messageBus.appHostRequest("systray/tooltip/set", PRODUCT_NAME + " " + gettextCatalog.getString("Your connection is unsecure"));
          break;

        case "Connecting":
          messageBus.appHostRequest("menu/set", {
            name: "toggleConnect",
            enabled: false,
            text: gettextCatalog.getString("Connecting")
          });
          messageBus.appHostRequest("systray/tooltip/set", PRODUCT_NAME + " " + gettextCatalog.getString("Connecting"));
          break;

        case "Disconnecting":
          messageBus.appHostRequest("menu/set", {
            name: "toggleConnect",
            enabled: false,
            text: gettextCatalog.getString("Disconnecting")
          });
          messageBus.appHostRequest("systray/tooltip/set", PRODUCT_NAME + " " + gettextCatalog.getString("Disconnecting"));
          break;
      }
    });
    return menu;
  }]);
};

},{}],45:[function(require,module,exports){
"use strict";

Object.defineProperty(exports, "__esModule", {
  value: true
});
exports["default"] = void 0;

function _classCallCheck(instance, Constructor) { if (!(instance instanceof Constructor)) { throw new TypeError("Cannot call a class as a function"); } }

function _defineProperties(target, props) { for (var i = 0; i < props.length; i++) { var descriptor = props[i]; descriptor.enumerable = descriptor.enumerable || false; descriptor.configurable = true; if ("value" in descriptor) descriptor.writable = true; Object.defineProperty(target, descriptor.key, descriptor); } }

function _createClass(Constructor, protoProps, staticProps) { if (protoProps) _defineProperties(Constructor.prototype, protoProps); if (staticProps) _defineProperties(Constructor, staticProps); return Constructor; }

var DefaultConfigurator = /*#__PURE__*/function () {
  function DefaultConfigurator() {
    _classCallCheck(this, DefaultConfigurator);
  }

  _createClass(DefaultConfigurator, [{
    key: "getLabels",
    value: function getLabels() {
      return {
        ProductName: "Phantom VPN",
        BrandName: "Avira",
        SublogoTextPro: "Pro",
        ContextMenuName: "Avira Phantom VPN.",
        ProBadge: "PRO"
      };
    }
  }, {
    key: "client",
    get: function get() {
      return "vpn";
    }
  }, {
    key: "secret",
    get: function get() {
      return "32e230559c4e051d2e2dd4ea2479e0a356594304e2cccf7b4358c93094004179";
    }
  }, {
    key: "oeUrl",
    get: function get() {
      return "https://api.my.avira.com";
    }
  }, {
    key: "useForcedLogin",
    get: function get() {
      return false;
    }
  }, {
    key: "darkThemeAvailable",
    get: function get() {
      return true;
    }
  }, {
    key: "forceDarkTheme",
    get: function get() {
      return false;
    }
  }, {
    key: "hideAccountText",
    get: function get() {
      return false;
    }
  }, {
    key: "hideAboutMenuEntry",
    get: function get() {
      return false;
    }
  }, {
    key: "regionsOnlyNearestFree",
    get: function get() {
      return false;
    }
  }, {
    key: "showFeedbackOnDisconnect",
    get: function get() {
      return false;
    }
  }, {
    key: "showAboutLink",
    get: function get() {
      return false;
    }
  }, {
    key: "showQuitLink",
    get: function get() {
      return false;
    }
  }, {
    key: "showLogoutLink",
    get: function get() {
      return false;
    }
  }, {
    key: "allowLogout",
    get: function get() {
      return true;
    }
  }, {
    key: "showThemeSelection",
    get: function get() {
      return true;
    }
  }, {
    key: "useOsTheme",
    get: function get() {
      return true;
    }
  }, {
    key: "showAccountButton",
    get: function get() {
      return false;
    }
  }, {
    key: "showHelpButton",
    get: function get() {
      return true;
    }
  }, {
    key: "emailConfirmationNeeded",
    get: function get() {
      return true;
    }
  }, {
    key: "onlyProConnects",
    get: function get() {
      return false;
    }
  }, {
    key: "useOeAuth",
    get: function get() {
      return true;
    }
  }, {
    key: "openDashboardInUI",
    get: function get() {
      return false;
    }
  }]);

  return DefaultConfigurator;
}();

exports["default"] = DefaultConfigurator;

},{}],46:[function(require,module,exports){
"use strict";

module.exports = function (module) {
  module.factory('DiagnosticData', ['MessageBus', function (MessageBus) {
    var DiagnosticData = {
      userSelection: {
        connectionIssue: false,
        speedIssue: false,
        licenseIssue: false,
        otherIssue: false,
        description: ""
      },
      reference: ""
    };
    return DiagnosticData;
  }]);
};

},{}],47:[function(require,module,exports){
"use strict";

module.exports = function (module) {
  module.factory('$exceptionHandler', function () {
    return function (exception, cause) {
      console.log(exception.toString());
    };
  });
};

},{}],48:[function(require,module,exports){
"use strict";

function _classCallCheck(instance, Constructor) { if (!(instance instanceof Constructor)) { throw new TypeError("Cannot call a class as a function"); } }

function _defineProperties(target, props) { for (var i = 0; i < props.length; i++) { var descriptor = props[i]; descriptor.enumerable = descriptor.enumerable || false; descriptor.configurable = true; if ("value" in descriptor) descriptor.writable = true; Object.defineProperty(target, descriptor.key, descriptor); } }

function _createClass(Constructor, protoProps, staticProps) { if (protoProps) _defineProperties(Constructor.prototype, protoProps); if (staticProps) _defineProperties(Constructor, staticProps); return Constructor; }

module.exports = function (module) {
  module.factory('Features', ['MessageBus', function (MessageBus) {
    var Features = /*#__PURE__*/function () {
      function Features(messageBus) {
        _classCallCheck(this, Features);

        this.messageBus = messageBus;
        this.subscribeToFeatures();
        this.requestFeatures();
      }

      _createClass(Features, [{
        key: "subscribeToFeatures",
        value: function subscribeToFeatures() {
          var _this = this;

          this.messageBus.subscribe(this.messageBus.FEATURES, function (message) {
            _this.features = message.params;
          });
        }
      }, {
        key: "requestFeatures",
        value: function requestFeatures() {
          var _this2 = this;

          this.messageBus.request(this.messageBus.FEATURES, function (message) {
            _this2.features = message.result;
          });
        }
      }, {
        key: "getFeatures",
        get: function get() {
          if (this.features) {
            return this.features;
          }

          return {};
        }
      }]);

      return Features;
    }();

    return new Features(MessageBus);
  }]);
};

},{}],49:[function(require,module,exports){
"use strict";

function _classCallCheck(instance, Constructor) { if (!(instance instanceof Constructor)) { throw new TypeError("Cannot call a class as a function"); } }

function _defineProperties(target, props) { for (var i = 0; i < props.length; i++) { var descriptor = props[i]; descriptor.enumerable = descriptor.enumerable || false; descriptor.configurable = true; if ("value" in descriptor) descriptor.writable = true; Object.defineProperty(target, descriptor.key, descriptor); } }

function _createClass(Constructor, protoProps, staticProps) { if (protoProps) _defineProperties(Constructor.prototype, protoProps); if (staticProps) _defineProperties(Constructor, staticProps); return Constructor; }

module.exports = function (module) {
  module.factory('License', ['MessageBus', function (MessageBus) {
    var License = /*#__PURE__*/function () {
      function License(messageBus) {
        _classCallCheck(this, License);

        this.messageBus = messageBus;
        this.license = null;
        this.subscribeToUserLicense();
        this.requestUserLicense();
      }

      _createClass(License, [{
        key: "subscribeToUserLicense",
        value: function subscribeToUserLicense() {
          var _this = this;

          this.messageBus.subscribe(this.messageBus.USER_LICENSE, function (message) {
            _this.license = message.params;
            MessageBus.trigger(MessageBus.VPN_LICENSE_CHANGED);
          });
        }
      }, {
        key: "requestUserLicense",
        value: function requestUserLicense() {
          var _this2 = this;

          this.messageBus.request(this.messageBus.USER_LICENSE, function (message) {
            _this2.license = message.result;
            MessageBus.trigger(MessageBus.VPN_LICENSE_CHANGED);
          });
        }
        /**
         * Used as enum for possible license types.
         *
         * @readonly
         * @memberof License
         */

      }, {
        key: "getLicenseType",
        value: function getLicenseType() {
          return "Pro";
        }
      }, {
        key: "getLicense",
        value: function getLicense() {
          return this.license;
        }
      }, {
        key: "getTrafficInterval",
        value: function getTrafficInterval() {
          if (this.license) {
            return this.license.traffic_limit_interval;
          }

          return "monthly";
        }
      }, {
        key: "Type",
        get: function get() {
          return {
            Unregistered: "Unregistered",
            Registered: "Registered",
            Pro: "Pro"
          };
        }
      }]);

      return License;
    }();

    return new License(MessageBus);
  }]);
};

},{}],50:[function(require,module,exports){
"use strict";

var External = require('services/vpn-external');

module.exports = function (module) {
  module.factory('MessageBus', ['$timeout', 'gettext', function ($timeout, gettext) {
    var callbacks = {};
    var requestCallbacks = {};
    var onError = null;

    var uid = function uid() {
      var id = '';

      for (var i = 0; i < 5; i++) {
        id += Math.round(Math.random() * 10) + '';
      }

      return +id;
    };

    var messageBus = {
      EMBEDDED: External.EMBEDDED,
      CLOSE: External.CLOSE,
      ALL: '*',
      STATUS: 'status',
      FEATURES: 'features/get',
      IS_SANDBOXED: 'isSandBoxed/get',
      TRAFFIC: 'traffic/get',
      GETTRAFFIC: 'traffic/get',
      REGIONS: 'regions/get',
      GETREGIONS: 'regions/get',
      USER_LICENSE: 'users/currentUser/license',
      REGISTER_USER: 'registerUser',
      OPEN_DASHBOARD: 'openDashboard',
      DISCONNECT_TIMER: 'disconnectTimer',
      UPGRADE: 'upgrade',
      TRAFFIC_LIMIT_REACHED: 'trafficLimitReached',
      CONNECT: 'connect',
      DISCONNECT: 'disconnect',
      APPSETTINGS: 'appSettingsSubscription',
      APPSETTINGSGET: 'appSettings/get',
      APPSETTINGSSET: 'appSettings/set',
      USERSETTINGSGET: 'userSettings/get',
      USERSETTINGSSET: 'userSettings/set',
      OPEN_URL_IN_DEFAULT_BROWSER: 'openUrlInDefaultBrowser',
      WIFI_GET: 'wifis/get',
      WIFI_TRUST: 'wifis/trust',
      WIFI_UNTRUST: 'wifis/untrust',
      WIFI_DELETE: 'wifis/delete',
      TRACK_EVENT: "trackEvent",
      STORAGE_SET: 'storageSet',
      STORAGE_GET: 'storageGet',
      STORAGE_REMOVE: 'storageRemove',
      ACCESS_TOKEN: 'accessToken',
      REFRESH_ACCESS_TOKEN: 'refreshAccessToken',
      PRODUCT_CATALOGUE: 'productCatalogue',
      PURCHASE: 'purchase',
      RESTORE_PURCHASE: 'restorePurchase',
      PURCHASE_STATUS: 'purchaseStatus',
      USER_REGISTERED: 'userRegistered',
      TRANSLATED_STRINGS: 'translatedStrings',
      FAST_FEEDBACK_STRINGS: 'fastFeedbackStrings',
      OPEN_PAGE: 'openView',
      IP_ADDRESS_REFRESHED: 'ipAddressRefreshed',
      REFRESH_IP_ADDRESS: 'refreshIPAddress',
      START_DEVICE_PINGER: 'startDevicePinger',
      SEND_DEVICE_PING: "sendDevicePing",
      DISPLAY_RATE_ME: "displayRateMe",
      DISPLAY_FAST_FEEDBACK: "displayFastFeedback",
      DISPLAY_DATA_USAGE_POPUP: "displayDataUsagePopup",
      OPEN_APP_STORE: "openAppStore",
      SEND_FAST_FEEDBACK: "sendFastFeedback",
      NOT_NOW_FAST_FEEDBACK: "notNowFastFeedback",
      GET_PRO_DATA_USAGE: "getProDataUsage",
      NOT_NOW_DATA_USAGE: "notNowDataUsage",
      KEYCHAIN_ACCESS_GRANTED: "keychainAccessGranted",
      UI_VISIBLE: "uiVisible",
      ACTIVATE_TRIAL: "activateTrial",
      NEW_ACCESS_TOKEN: 'newAccessToken',
      LOGOUT: 'logout',
      QUIT: 'quit',
      DIAGNOSTIC_DATA_SEND: 'diagnostics/send',
      DIAGNOSTIC_DATA_STATUS: 'diagnostics/get',
      DIAGNOSTIC_DATA_LAST_REFERENCE: 'diagnostics/lastReference',
      SYSTEM_SETTINGS: 'systemSettings',
      SYSTEM_SETTINGS_CHANGED: 'systemSettingsChanged',
      DISPLAY_SETTINGS_GET: "displaySettings/get",
      DISPLAY_SETTINGS_SET: "displaySettings/set",
      THEME_SELECTION_GET: "themeSelection/get",
      THEME_SELECTION_SET: "themeSelection/set",
      LOGIN: "login",
      LICENSE_UPDATE: "license/update",
      SERVICE_READY: "serviceReady",
      USER_PROFILE: "userProfile/get",
      USER_PROFILE_CHANGED: "userProfileChanged",
      UI_SETTINGS_CHANGED: "uiSettingsChanged",
      // Commands used internally by the GUI all have a 'vpn:' prefix
      // Not actually required, but good to differentiate from commands going to VPN
      SELECTED_REGION_CHANGED: 'vpn:regionchanged',
      REGIONS_UPDATED: 'vpn:regionsupdated',
      VPN_USER_REGISTERED: 'vpn:userregistered',
      VPN_UPDATE_REGISTRATION_STATUS: 'vpn:updateRegistrationStatus',
      VPN_ERROR: 'vpn:error',
      VPN_REQUEST_ERROR: 'vpn:requestError',
      VPN_STATUS_CHANGED: 'vpn:statusChanged',
      VPN_TRAFFIC_LIMIT_REACHED: 'vpn:trafficlimitreached',
      VPN_CHANGEVIEW: 'vpn:settingsButtonClicked',
      VPN_VIEW: 'vpn:view',
      VPN_VIEW_BACK: 'vpn:viewBack',
      VPN_RENEW: 'vpn:renew',
      VPN_FEATURES: 'vpn:features',
      VPN_IS_SANDBOXED: 'vpn:isSandboxed',
      VPN_REMOVE_TRAFFIC_LIMIT: 'vpn:removeTrafficLimit',
      VPN_ENABLE_HEADER: 'vpn:enableHeader',
      VPN_UPDATE_STRINGS: 'vpn:updateStrings',
      VPN_CHANGE_THEME: 'vpn:changeTheme',
      VPN_LICENSE_CHANGED: 'vpn:licenseChanged',
      service: "VPN",
      subscribe: function subscribe(method, callback) {
        var envelope = {
          service: messageBus.service,
          subscribe: method
        };
        var mmethod = 'vpn:' + method;
        callbacks[mmethod] = callbacks[mmethod] || [];
        var i = callbacks[mmethod].push(callback) - 1;
        External.send(envelope);
      },
      reestablishConnection: function reestablishConnection() {
        External.trace("UI: Reestablishing connection...");

        for (var callback in callbacks) {
          if (callbacks.hasOwnProperty(callback) && callback.indexOf('vpn:') === 0) {
            var envelope = {
              service: messageBus.service,
              subscribe: callback.substring(4)
            };
            External.send(envelope);
          }
        }
      },
      unsubscribeAll: function unsubscribeAll() {
        External.trace("UI: Unsubscribing from all methods");

        for (var m in callbacks) {
          if (callbacks.hasOwnProperty(m) && m.indexOf('vpn:') === 0) {
            this.unsubscribe(m.substring(4));
          }
        }
      },
      unsubscribe: function unsubscribe(method) {
        var envelope = {
          service: messageBus.service,
          unsubscribe: method
        };
        delete callbacks['vpn:' + method];
        External.send(envelope);
        External.trace("UI: Unsubscribed from method " + method);
      },
      request: function request(method, callback) {
        var args = Array.prototype.slice.call(arguments);
        var parameters = null;
        var hiddenParams = false;

        if (args.length >= 3) {
          parameters = callback;
          callback = args[2];

          if (args.length === 4 && typeof args[3] === "boolean") {
            hiddenParams = args[3];
          }
        }

        var envelope = {
          service: messageBus.service,
          message: {
            method: method,
            id: uid()
          }
        };

        if (parameters) {
          envelope.message.params = parameters;
        }

        if (hiddenParams) {
          envelope.message.hiddenParams = hiddenParams;
        }

        requestCallbacks[envelope.message.id + ""] = callback;
        External.send(envelope);
      },
      appHostRequest: function appHostRequest(method, param, callback) {
        var envelope = {
          service: "WebAppHost",
          message: {
            method: method,
            id: uid()
          }
        };

        if (param) {
          envelope.message.params = param;
        }

        requestCallbacks[envelope.message.id + ""] = callback;
        External.send(envelope);
      },
      send: function send(method, data) {
        if (method === this.LOG) {
          External.trace("UI: " + data);
        }
      },
      trace: function trace(text) {
        External.trace("UI: " + text);
      },
      setErrorCallback: function setErrorCallback(callback) {
        onError = callback;
      },
      on: function on(event, callback) {
        callbacks[event] = callbacks[event] || [];
        callbacks[event].push(callback);
      },
      trigger: function trigger() {
        var args = Array.prototype.slice.call(arguments);

        if (args.length === 0) {
          return;
        }

        var event = args[0];
        args.shift();
        var listeners = callbacks[event] || [];
        $timeout(function () {
          for (var i = 0; i < listeners.length; i++) {
            listeners[i].apply(null, args);
          }
        }, 0);
      }
    };
    messageBus.subscribe("connectionReestablished", messageBus.reestablishConnection);
    External.onMessage(function (message) {
      //External.trace("On message received " + JSON.stringify(message));
      var listeners = [];

      if (message.id) {
        if (message.error && message.error.code === -32604) {
          messageBus.trigger(messageBus.VPN_REQUEST_ERROR, gettext("Failed to connect to the service."));
          return;
        }

        var l = requestCallbacks[message.id + ""];
        delete requestCallbacks[message.id + ""];
        listeners = !l ? [] : [l];
      } else if (message.method) {
        listeners = callbacks['vpn:' + message.method] || [];
      }

      if (listeners.length === 0) {
        return;
      }

      var i;

      if (message.method && message.method === External.CLOSE) {
        External.trace("UI: Message close listeners callback");

        for (i = 0; i < listeners.length; i++) {
          listeners[i](message);
        }
      } else {
        $timeout(function () {
          for (i = 0; i < listeners.length; i++) {
            listeners[i](message);
          }
        }, 0);
      }
    });
    return messageBus;
  }]);
};

},{"services/vpn-external":56}],51:[function(require,module,exports){
"use strict";

var OAuth = require('oe-oauth');

var isWindowsClient = window.external !== undefined && typeof window.external.SendMessage !== "undefined";

module.exports = function (module) {
  module.factory('oe', ['MessageBus', 'AppHostStorage', 'Configurator', function (MessageBus, AppHostStorage, Configurator) {
    if (!Configurator.useOeAuth) {
      return null;
    }

    var logger = function logger(message) {
      MessageBus.trace(message);
    };

    if (isWindowsClient) {
      var mock = {};

      mock.registerDeviceIfNotLoggedIn = function () {
        return new Promise(function (resolve) {
          return resolve(true);
        });
      };

      return mock;
    }

    var oeAuth = new OAuth(Configurator.oeUrl, Configurator.client, Configurator.secret, AppHostStorage, logger);
    var device_data = {};
    var app_data = {};

    oeAuth.sendToken = function () {
      return oeAuth.accessToken().then(function (token) {
        MessageBus.request(MessageBus.ACCESS_TOKEN, token, function () {});
      });
    };

    oeAuth.getDeviceData = function () {
      return device_data;
    };

    oeAuth.getAppData = function () {
      return app_data;
    };

    MessageBus.subscribe(MessageBus.ACCESS_TOKEN, function () {
      oeAuth.sendToken();
    });
    MessageBus.subscribe(MessageBus.REFRESH_ACCESS_TOKEN, function () {
      oeAuth.refresh();
    });

    var initDeviceData = function initDeviceData() {
      return new Promise(function (resolve) {
        MessageBus.request("deviceData/get", function (m) {
          device_data = m.result;
          resolve();
        });
      });
    };

    var initAppData = function initAppData() {
      return new Promise(function (resolve) {
        MessageBus.request("appData/get", function (m) {
          app_data = m.result;
          resolve();
        });
      });
    };

    oeAuth.requestPermanentToken = function () {
      return oeAuth.accessToken().then(function (token) {
        if (token && token !== "") {
          MessageBus.trace("Permanent taken already cached in storage.");
          return new Promise(function (resolve) {
            return resolve(true);
          });
        }

        MessageBus.trace("Requesting permanent anonym token.");
        return oeAuth.registerDevice(oeAuth.getDeviceData(), oeAuth.getAppData());
      }).then(function () {
        return oeAuth.sendToken();
      })["catch"](function (error) {
        MessageBus.trace("Failed to register device: ".concat(JSON.stringify(error)));
      });
    };

    var initService = function initService() {
      MessageBus.trace("Initializing OE Service...");
      initDeviceData().then(function () {
        return initAppData();
      }).then(function () {
        return oeAuth.requestPermanentToken();
      });
    };

    initService();

    oeAuth.sendGdprConsentToConnect = function () {
      MessageBus.trace("Sending the Gdpr consent to Connect Backend...");
      oeAuth.registerDevice(oeAuth.getDeviceData(), oeAuth.getAppData()).then(function () {
        oeAuth.storeGdprConsent();
        MessageBus.request(MessageBus.START_DEVICE_PINGER, function () {});
      })["catch"](function (error) {
        MessageBus.trace(JSON.stringify(error));
      });
    };

    var sendGdprPing = function sendGdprPing(token) {
      if (token && token !== "") {
        oeAuth.sendDevicePing(token).then(function (result) {
          MessageBus.trace("Sent device ping for current device.");
        })["catch"](function (error) {
          MessageBus.trace("Failed to send device ping. " + JSON.stringify(error));
        });
      }
    };

    oeAuth.sendGdprPingToConnect = function () {
      MessageBus.trace("Sending device ping to Connect Backend...");

      if (isWindowsClient) {
        MessageBus.request(MessageBus.ACCESS_TOKEN, function (message) {
          sendGdprPing(message.result);
        });
      } else {
        oeAuth.accessToken().then(function (token) {
          sendGdprPing(token);
        });
      }
    };

    MessageBus.subscribe(MessageBus.SEND_DEVICE_PING, function () {
      return oeAuth.sendGdprPingToConnect();
    });
    MessageBus.subscribe(MessageBus.NEW_ACCESS_TOKEN, function (message) {
      MessageBus.trace("Received new access token. Reinitializing oeauth library...");
      var payload = JSON.parse(message.params);
      oeAuth.handleOAuthResponse(payload, '').then(function () {
        return oeAuth.sendToken();
      });
    });
    return oeAuth;
  }]);
};

},{"oe-oauth":3}],52:[function(require,module,exports){
"use strict";

///<reference path="../../../Scripts/typings/angularjs/angular.d.ts"/>
var VpnApp;

(function (VpnApp) {
  var Services;

  (function (Services) {
    'use strict';

    var RegionList = function () {
      function RegionList(messageBus, settings) {
        var _this = this;

        this.messageBus = messageBus;
        this.settings = settings;

        this.updateLatency = function () {
          if (_this.regions && _this.regions.length > 0 && _this.regions[0].latency == "") {
            _this.messageBus.request("regions/latency");
          }
        };

        this.adjustIconId = function (region) {
          region.iconId = region.id;
          var idx = region.iconId.indexOf('_');

          if (idx > 1) {
            region.iconId = region.iconId.substr(0, idx);
          }
        };

        this.getRegions = function (message) {
          _this.regions = message.result.regions;

          _this.messageBus.request("regions/latency");

          _this.regions.forEach(function (region) {
            _this.adjustIconId(region);
          });

          _this.messageBus.trigger(_this.messageBus.REGIONS_UPDATED, _this.regions);

          _this.selected = null;
          var defregion = message.result["default"];

          if (_this.settings.data.selectedRegion) {
            _this.regions.forEach(function (region) {
              if (_this.settings.data.selectedRegion === region.id) {
                _this.selected = region;
                return false; // stop looking further
              }

              return true;
            });
          }

          if (!_this.selected) {
            _this.regions.forEach(function (region) {
              if (defregion === region.id) {
                _this.selected = region;
                return false; // stop looking further 
              }

              return true;
            });
          }

          if (!_this.selected) {
            _this.selected = _this.regions[0];
          }

          _this.messageBus.trigger(_this.messageBus.SELECTED_REGION_CHANGED, _this.selected);
        };

        messageBus.request(messageBus.GETREGIONS, this.getRegions);
        messageBus.subscribe(messageBus.REGIONS, function () {
          messageBus.request(messageBus.GETREGIONS, _this.getRegions);
        });
        messageBus.subscribe(messageBus.UI_SETTINGS_CHANGED, function (message) {
          try {
            var params = message ? message.params : null;
            var selectedRegion = params ? params.selectedRegion : null;

            if (selectedRegion && selectedRegion !== _this.selected.id) {
              _this.regions.forEach(function (region) {
                if (region.id === selectedRegion) {
                  _this.select(region);

                  return;
                }
              });
            }
          } catch (error) {}
        });
        messageBus.subscribe("latency", function (message) {
          _this.regions.forEach(function (region) {
            if (region.id == message.params.id) {
              region.latency = message.params.latency > 0 ? String(message.params.latency) + " ms" : "";
            }
          });
        });
      }

      RegionList.prototype.select = function (region) {
        this.selected = region;
        this.settings.data.selectedRegion = region.id;
        this.messageBus.trigger(this.messageBus.SELECTED_REGION_CHANGED, region);
      };

      return RegionList;
    }();

    function factory(messageBus, settings) {
      return new RegionList(messageBus, settings);
    }

    factory.$inject = ['MessageBus', 'Settings'];
    angular.module('LauncherApp').factory('RegionList', factory);
  })(Services = VpnApp.Services || (VpnApp.Services = {}));
})(VpnApp || (VpnApp = {}));

},{}],53:[function(require,module,exports){
"use strict";

module.exports = function (module) {
  module.factory('Settings', ['MessageBus', function (MessageBus) {
    var Settings = {
      data: {}
    };
    var self = Settings;
    MessageBus.appHostRequest("settings/get", null, function (message) {
      try {
        self.data = message.result ? message.result : {};
        MessageBus.trigger("SettingsChanged", self);
      } catch (error) {
        MessageBus.send(MessageBus.LOG, "Error in receiving Settings...");
      }
    });
    return Settings;
  }]);
};

},{}],54:[function(require,module,exports){
"use strict";

module.exports = function (module) {
  module.factory('Telemetry', ['MessageBus', function (MessageBus) {
    var Telemetry = {
      UPGRADE_CLICKED: "Upgrade Clicked",
      LOGIN_SUCCESSFUL: "Login",
      LOGIN_STARTED: "Login Started",
      REGISTRATION_STARTED: "Registration Started",
      LOGOUT: "Logout",
      PURCHASE_STARTED: "Purchase Started",
      UPGRADE_POSTPONED: "Upgrade Postponed",
      TRIAL_ACTIVATION_STARTED: "Trial Activation Started",
      TRIAL_ACTIVATION_POSTPONED: "Trial Activation Postponed",
      GUI_OPENED: "Gui Opened",
      TRIAL_BANNER_CLICKED: "Trial Banner Clicked",
      INSTALL_SUCCESS: "Install Success",
      USER_FEEDBACK_HIDDEN: "User Feedback Hidden",
      VIEW_OPENED: "View Opened",
      WAITING_CANCELED: "Waiting Canceled",
      WAITING_TIME_EXPIRED: "Waiting Time Expired",
      sendEvent: function sendEvent(name) {
        var additionalProperties = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : null;
        var trackingEvent = {
          "eventId": name,
          "properties": additionalProperties
        };
        MessageBus.request(MessageBus.TRACK_EVENT, trackingEvent, function () {});
      }
    };
    return Telemetry;
  }]);
};

},{}],55:[function(require,module,exports){
"use strict";

function TestScriptingObject() {
  this.__messagecallback = function () {};

  this.__onCloseCallBack = function () {};

  this.requests = {};
  this.responses = {};

  this.__onRequestCallback = function () {};
}

TestScriptingObject.prototype.Trace = function (message) {
  console.log(message);
};

TestScriptingObject.prototype.SendMessage = function (envelope) {
  console.log("Sending message...");
  console.log(envelope);
  var envelope = JSON.parse(envelope);

  if (envelope.message && !isNaN(envelope.message.id)) {
    this.requests[envelope.message.method] = envelope.message; // ignore standart processing if callback can process it

    this.__onRequestCallback(envelope.message);

    if (this.responses[envelope.message.method]) {
      var message = envelope.message;
      this.respond(message.method, this.responses[message.method], message.$$delay);
    }
  } else {
    this.__onRequestCallback({
      method: envelope.subscribe
    });
  }
};

TestScriptingObject.prototype.RegisterRequestCallback = function (callback) {
  this.__onRequestCallback = callback;
};

TestScriptingObject.prototype.RegisterMessageCallback = function (callback) {
  this.__messagecallback = callback;
};

TestScriptingObject.prototype.RegisterOnClose = function (callback) {
  this.__onCloseCallBack = callback;
};

TestScriptingObject.prototype.mimicReceive = function (envelope) {
  this.__messagecallback(JSON.stringify(envelope));
};

TestScriptingObject.prototype.close = function () {
  this.__onCloseCallBack();

  window.location.reload();
};

TestScriptingObject.prototype.respond = function (method, message, delay) {
  if (isNaN(delay)) {
    if (!isNaN(message.$$delay)) {
      delay = message.$$delay;
    } else {
      delay = 0;
    }
  }

  var request = this.requests[method];

  if (request) {
    message.id = request.id;
  } else {
    message.$$delay = delay;
    this.responses[method] = message;
    return;
  }

  var envelope = {
    message: message,
    service: ""
  };
  var self = this;
  setTimeout(function () {
    self.mimicReceive(envelope);
  }, delay);
};

module.exports = new TestScriptingObject();

},{}],56:[function(require,module,exports){
"use strict";

var isWindowsClient = window.external !== undefined && typeof window.external.SendMessage !== "undefined";
var isUWPClient = window.UWPAppController !== undefined;
var isMacClient = window.MacAppController !== undefined;
var isSimulator = !(isWindowsClient || isMacClient || isUWPClient);

function ExternalMessageBus() {
  var self = this;

  this.__callback = function () {
    console.log("!!!!!! Fake callback...");
  };

  this.isSimulator = isSimulator;
  this.scriptingObject = null;

  if (isWindowsClient) {
    console.log("Initializing Windows client GUI ...");
    this.scriptingObject = window.external; // hook to provide ability to read local files without restrictions 

    window.XMLHttpRequest = function () {
      return require('services/xmlHttpRequest');
    };
  } else if (isUWPClient) {
    console.log("Initializing UWP client GUI ...");
    this.scriptingObject = {};

    this.scriptingObject.SendMessage = function (message) {
      window.UWPAppController.sendMessage(message);
    };

    this.scriptingObject.RegisterMessageCallback = function (callback) {
      window.UWPAppController.addEventListener("messagecallback", function (arg) {
        callback(arg);
      });
    };

    this.scriptingObject.RegisterOnClose = function (callback) {};

    this.scriptingObject.Trace = function (message) {
      window.UWPAppController.trace(message);
    };
  } else if (isMacClient) {
    console.log("Initializing Mac client GUI ...");
    this.scriptingObject = {};

    this.scriptingObject.SendMessage = function (message) {
      window.MacAppController.SendMessage_(message);
    };

    this.scriptingObject.RegisterMessageCallback = function (callback) {
      window.MacAppController.RegisterMessageCallback_(callback);
    };

    this.scriptingObject.RegisterOnClose = function (callback) {
      window.MacAppController.RegisterOnClose_(callback);
    };

    this.scriptingObject.Trace = function (message) {
      window.MacAppController.Trace_(message);
    };
  } else {
    console.log("Initializing simulator GUI ...");
    this.scriptingObject = require('services/testScriptingObject');
    this.testScriptingObject = this.scriptingObject;
  }

  var messageCallback = function messageCallback(envelope) {
    try {
      var envelope = JSON.parse(envelope);
      var message = envelope.message;

      if (message.method !== null || message.id !== null) {
        self.__callback(message);
      }
    } catch (error) {
      console.log(error);
    }
  }; //this is needed for MacApp


  messageCallback.call = messageCallback;
  this.scriptingObject.RegisterMessageCallback(messageCallback);
  this.scriptingObject.RegisterOnClose(function () {
    try {
      self.scriptingObject.Trace("Closing the window...");
      var message = {
        method: ExternalMessageBus.prototype.CLOSE
      };

      self.__callback(message);

      self.scriptingObject.Trace("callback message:" + message);
    } catch (error) {
      self.scriptingObject.Trace("callback error: " + error.message); // console.log(error);
    }
  });
}

ExternalMessageBus.prototype.trace = function (envelope) {
  this.scriptingObject.Trace(envelope);
};

ExternalMessageBus.prototype.send = function (envelope) {
  try {
    envelope = JSON.stringify(envelope);
    this.scriptingObject.SendMessage(envelope);
  } catch (error) {
    console.log(error);
  }
};

ExternalMessageBus.prototype.onMessage = function (callback) {
  console.log('prototype.onMessage: ' + callback);

  if (typeof callback == 'function' || false) {
    this.__callback = callback;
  }
};

ExternalMessageBus.prototype.CLOSE = '__close__';
ExternalMessageBus.prototype.EMBEDDED = isWindowsClient;
module.exports = new ExternalMessageBus();

},{"services/testScriptingObject":55,"services/xmlHttpRequest":58}],57:[function(require,module,exports){
"use strict";

///<reference path="../../../Scripts/typings/angularjs/angular.d.ts"/>
var isWindowsClient = window.external !== undefined && typeof window.external.SendMessage !== "undefined";
var VpnApp;

(function (VpnApp) {
  var Services;

  (function (Services) {
    'use strict';

    var VpnService = function () {
      function VpnService(messageBus, license, features) {
        var _this = this;

        this.messageBus = messageBus;
        this.license = license;
        this.features = features;

        this.connect = function (region, triggerSource) {
          region.trigger_source = _this.getTriggerSource(triggerSource);
          _this.currentRegion = region;
          var feat = _this.features.getFeatures;

          if (isWindowsClient && _this.license.getLicenseType() != _this.license.Type.Pro && feat && feat.waitingWindow && feat.waitingWindow.enabled) {
            _this.messageBus.trigger(_this.messageBus.VPN_CHANGEVIEW, "waitingWindowView");
          } else {
            _this.messageBus.request(_this.messageBus.CONNECT, region, function () {});
          }
        };

        this.forceConnect = function (region) {
          _this.currentRegion = region;

          _this.messageBus.request(_this.messageBus.CONNECT, region, function () {});
        };

        this.connectToLastRegion = function (triggerSource) {
          _this.currentRegion.trigger_source = _this.getTriggerSource(triggerSource);

          _this.messageBus.request(_this.messageBus.CONNECT, _this.currentRegion, function () {});
        };

        this.cancel = function () {
          if (_this.status != "Connecting") return;
          _this.disconnectCallback = null;

          _this.messageBus.request(_this.messageBus.DISCONNECT, "GuiOtherDisconnect", function () {});
        };

        this.isDisconnected = function () {
          return _this.status == "Disconnected";
        };

        this.disconnect = function (callback, source) {
          _this.disconnectCallback = callback;

          _this.messageBus.request(_this.messageBus.DISCONNECT, source, function () {});
        };

        this.statusChanged = function (status) {
          _this.status = status;

          _this.messageBus.trace("VpnService got status:" + _this.status);

          if (_this.status == "Disconnected" && _this.disconnectCallback) {
            _this.messageBus.trace("Calling disconnect callback");

            _this.disconnectCallback();

            _this.disconnectCallback = null;
          }
        };

        this.setCurrentRegion = function (region) {
          _this.currentRegion = region;
        };

        messageBus.on(messageBus.VPN_STATUS_CHANGED, this.statusChanged);
        messageBus.request(messageBus.STATUS, function (message) {
          _this.status = message.result.status;

          _this.messageBus.trace("VpnService initial status: " + _this.status);

          _this.messageBus.trigger(messageBus.VPN_STATUS_CHANGED, _this.status);
        });
      }

      VpnService.prototype.getTriggerSource = function (triggerSource) {
        return triggerSource ? triggerSource : "";
      };

      return VpnService;
    }();

    function factory(messageBus, license, features) {
      return new VpnService(messageBus, license, features);
    }

    factory.$inject = ['MessageBus', 'License', 'Features'];
    angular.module('LauncherApp').factory('VpnService', factory);
  })(Services = VpnApp.Services || (VpnApp.Services = {}));
})(VpnApp || (VpnApp = {}));

},{}],58:[function(require,module,exports){
"use strict";

function CrossOriginRequest() {
  // this http request object does not have cross origin restrictions
  // and can work with local files
  this.xhr = new ActiveXObject("MSXML2.XMLHTTP.6.0");
  var obj = this;

  this.xhr.onreadystatechange = function () {
    obj.readyState = obj.xhr.readyState;

    if (obj.xhr.readyState == 4) {
      obj.propogateProperty();
    } // use status == 200 to know if the request was successfully


    if (obj.xhr.readyState === 4 && (obj.xhr.status === 200 || obj.xhr.status === 201 || obj.xhr.status === 202 || obj.xhr.status === 0)) {
      // since files are local -> status = 0 , no webserver here
      if (obj.onload) {
        obj.onload();
      }
    } // If request fail show progress bar in red


    if (obj.xhr.readyState === 4 && (obj.xhr.status === 404 || obj.xhr.status === 400 || obj.xhr.status === 403)) {
      if (obj.onerror) {
        obj.onerror();
      }
    }

    if (obj.xhr.readyState === 0) {
      if (obj.onabort) {
        obj.onabort();
      }
    }

    if (obj.onreadystatechange) {
      obj.onreadystatechange();
    }
  };

  this.propogateProperty = function () {
    this.responseBody = this.xhr.responseBody;
    this.responseStream = this.xhr.responseStream;
    this.responseText = this.xhr.responseText;
    this.responseXML = this.xhr.thisresponseXML;
    this.status = this.xhr.status;
    this.statusText = this.xhr.statusText;
  };
}

CrossOriginRequest.prototype.open = function (bstrMethod, bstrUrl, varAsync, bstrUser, bstrPassword) {
  return this.xhr.open(bstrMethod, bstrUrl, varAsync, bstrUser, bstrPassword);
};

CrossOriginRequest.prototype.send = function (varBody) {
  return this.xhr.send(varBody);
};

CrossOriginRequest.prototype.setRequestHeader = function (bstrHeader, bstrValue) {
  return this.xhr.setRequestHeader(bstrHeader, bstrValue);
};

CrossOriginRequest.prototype.getAllResponseHeaders = function () {
  return this.xhr.getAllResponseHeaders();
};

module.exports = new CrossOriginRequest();

},{}],59:[function(require,module,exports){
"use strict";

angular.module('LauncherApp').run(['gettextCatalog', function (gettextCatalog) {
  /* jshint -W100 */
  gettextCatalog.setStrings('de_DE', {
    "0 bytes": "0 Bytes",
    "<span class=\"privacyDescription__text__bold\">We don't log your online activity or any information that can link you to any action</span>, such as downloading a file, or visiting a particular website.": "<span class=\"privacyDescription__text__bold\">Wir zeichnen keine Ihrer Online-Aktivitäten oder Informationen auf</span>, wie beispielsweise das Herunterladen von Dateien oder der Besuch von bestimmten Webseiten.",
    "A new driver has been installed, please restart your computer to complete the installation": "Bitte starten Sie den Computer neu, bevor Sie Ihre Verbindung sichern.",
    "About": "Info",
    "Access": "Weiter",
    "Account details": "Kontodaten",
    "Agree and continue": "Akzeptieren und fortfahren",
    "All free slots are currently taken.": "Aktuell keine freien Server-Kapazitäten",
    "Already have an account?": "Sie haben bereits ein Konto?",
    "Always Allow": "Immer erlauben",
    "Application logs": "Anwendungsprotokolle",
    "Auto-connect VPN": "Automatisch mit VPN verbinden",
    "Auto-connect VPN for Wi-Fi networks": "In WLANs automatisch mit VPN verbinden",
    "Automatically secure untrusted Wi-Fi networks": "Bei unsicheren WLAN-Netzen automatisch via VPN verbinden",
    "Avira Phantom VPN Pro released!": "Avira Phantom VPN Pro ist da!",
    "Back": "Zurück",
    "Block all internet traffic if VPN connection drops": "Gesamten Internetverkehr blockieren, wenn die VPN-Verbindung getrennt wird",
    "Block intrusive ads": "Aufdringliche Werbung blockieren",
    "Block malicious sites and content": "Schädliche Websites und Inhalte blockieren",
    "Buy": "Kaufen",
    "Buy unlimited traffic": "Unbegrenzten Datenverkehr kaufen",
    "By proceeding, you are accepting the <a href=\"#\" ng-click=\"openEula()\">End User License Agreement (EULA)</a>, and the <a href=\"#\" ng-click=\"openTermsAndConditions()\">Terms and Conditions</a>. Avira is fulfilling its duties to provide information in accordance with Articles 13 and 14 of the General Data Protection Regulation (GDPR) with the contents of the Privacy Policy and access thereto. You can find our Privacy Policy here: <a href=\"#\" ng-click=\"openPrivacyAndPolicy()\">{{getPrivacyPolicyLink()}}</a>": "Indem Sie fortfahren, bestätigen Sie, dass Sie die <a href=\"#\" ng-click=\"openEula()\">Endbenutzer-Lizenzvereinbarung (EULA)</a> und  unsere <a href=\"#\" ng-click=\"openTermsAndConditions()\">Geschäftsbedingungen</a> gelesen und akzeptiert haben. Durch die Inhalte der Datenschutzhinweise und den Zugang zu diesen kommen wir unseren Informationspflichten nach Art. 13 bzw. 14 DSGVO nach. Unsere Datenschutzhinweise finden Sie hier: <a href=\"#\" ng-click=\"openPrivacyAndPolicy()\">{{getPrivacyPolicyLink()}}</a>",
    "Cancel": "Abbrechen",
    "Cannot resolve host address.": "Host-Adresse konnte nicht aufgelöst werden.",
    "Change later in Settings": "Später in Einstellungen ändern",
    "Check your inbox to confirm it's you. Make sure to look into your spam and junk folders as well.": "Postfach prüfen und E-Mail-Adresse bestätigen. Sehen Sie auch im Spam-/Junk-Ordner nach.",
    "Choose your color theme": "Wählen Sie Ihr Farb-Design",
    "Code": "Code",
    "Collect diagnostic data": "Senden",
    "Confirmation": "Senden erfolgreich",
    "Connect": "Verbinden",
    "Connecting": "Verbindungsaufbau",
    "Connecting VPN": "Verbindungsaufbau",
    "Connecting...": "Verbindung wird aufgebaut ...",
    "Connection problem": "Verbindungsproblem",
    "Connection to server lost.": "Die Verbindung zum Server ging verloren.",
    "Continue": "Weiter",
    "Copied to clipboard": "In Zwischenablage kopiert",
    "Copy": "Kopieren",
    "Dark": "Dunkel",
    "Dark theme": "Dunkles Design",
    "Diagnostics": "Diagnose",
    "Disconnect": "Trennen",
    "Disconnecting": "Verbindung wird getrennt",
    "Disconnecting...": "Verbindung wird getrennt ...",
    "Display settings": "Anzeige-Einstellungen",
    "Don't have an account yet?": "Kein Konto?",
    "Don't send": "Nicht senden",
    "Don't show again": "Nicht wieder anzeigen",
    "Don't show this again": "Nicht mehr anzeigen",
    "Don't want to wait? Upgrade now": "Warum warten? Upgrade starten",
    "Done": "Fertig",
    "Email address": "E-Mail-Adresse",
    "Email confirmation": "E-Mail-Bestätigung",
    "Email sent": "E-Mail gesendet",
    "Enjoy your unlimited data volume.<br/>Remaining days: {0}": "Unbegrenztes Datenvolumen jetzt nutzen –<br/>nur noch {0} Tage lang!",
    "Enter a valid email address first": "Geben Sie eine gültige E-Mail-Adresse ein",
    "Enter a valid password": "Geben Sie ein gültiges Passwort ein",
    "Enter a valid verification code.": "Geben Sie einen gültigen Verifizierungscode ein.",
    "Enter your Mac password and choose <span class=\"privacyDescription__text__bold\">Always allow</span> in the next window to continue.": "Bitte geben Sie Ihr Mac Passwort ein und wählen Sie im nächsten Fenster <span class=\"privacyDescription__text__bold\">Immer erlauben</span> aus, um fortzufahren.",
    "Establishing connection...": "Verbindung wird aufgebaut ...",
    "Exit": "Beenden",
    "Failed to connect to the service.": "Die Verbindung zum Dienst konnte nicht hergestellt werden.",
    "Failed to connect using UDP protocol. Retrying with TCP protocol.": "Verbindung mittels UDP-Protokoll fehlgeschlagen. Erneut versuchen mittels TCP-Protokoll.",
    "Failed to establish the VPN connection. Please try again.": "Verbindung kann nicht hergestellt werden. Bitte versuchen Sie es erneut.",
    "Failed to purchase. Please try again later.": "Beim Kauf ist ein Fehler aufgetreten. Bitte versuchen Sie es später erneut.",
    "Fatal error.": "Schwerer Fehler.",
    "Fill in this field.": "Füllen Sie dieses Feld aus.",
    "For security reasons your account has been blocked. Please access Avira Connect to unblock it.": "Ihr Konto wurde gesperrt. Sie können es einfach durch Zugriff auf Avira Connect entsperren.",
    "Forgot your password?": "Passwort vergessen?",
    "Get 3 Months Free": "Pro für 3 Monate sichern",
    "Get Pro": "Pro kaufen",
    "Get all the protection you need.": "Der Rundumschutz, den Sie brauchen.",
    "Get for this occasion your three-month free trial. Thank you very much for helping us in beta stage by using and testing our product.": "Jetzt gratis dreimonatige Testversion holen! Danke, dass Sie das Produkt getestet und geholfen haben, es zu verbessern.",
    "Get started": "Starten",
    "Get support": "Support erhalten",
    "Get unlimited data volume now.": "Jetzt unbegrenzt sicher surfen.",
    "Help Avira improve its products and services by automatically sending daily anonymous diagnostic and usage data.": "Durch tägliche automatische Übermittlung anonymer Diagnose- und Nutzungsdaten können Sie uns helfen, Avira Produkte und Dienste zu verbessern.",
    "Help us improve": "Helfen Sie uns, besser zu werden",
    "I entered a wrong email": "Falsche E-Mail-Adresse eingegeben",
    "IP Address:": "IP-Adresse",
    "IPSec traffic is blocked. Please contact your network administrator.": "IPSec-Protokoll blockiert. Wenden Sie sich an den Netzwerkadministrator.",
    "In case of technical issues you can collect diagnostic data and send a report to the developers.": "Haben Sie technische Probleme? Wir helfen Ihnen gern. Senden Sie uns einen Bericht und wir kümmern uns um alles andere.",
    "Invalid credentials": "Ungültige Zugangsdaten",
    "Last report sent:": "Letzter Bericht gesendet am:",
    "Launch at system start": "Anwendung bei Systemstart ausführen",
    "Length of subscription: 1 month/1 year<br><br> \n                    Price of subscription: 4,95€ per month (Mac only) – 7,95€ per month (all devices) – 59,95€ per year (all devices).<br><br>\n                    \n                    Payment will be charged to iTunes Account at confirmation of purchase. Subscription automatically renews unless auto-renew is turned off at least 24-hours before the end of the current period. Account will be charged for renewal within 24-hours prior to the end of the current period, and identify the cost of the renewal. Subscriptions may be managed by the user and auto-renewal may be turned off by going to the user's Account Settings after purchase. Any unused portion of a free trial period, if offered, will be forfeited when the user purchases a subscription to that publication, where applicable.": "Länge des Abonnements: 1 Monat / 1 Jahr ‒ Preis des Abonnements: 4,95 € pro Monat (nur Mac) ‒ 7,95 € pro Monat (alle Geräte) ‒ 59,95 € pro Jahr (alle Geräte). Bei Kaufbestätigung wird das iTunes-Konto mit dem Kaufbetrag belastet. Das Abonnement verlängert sich automatisch, es sei denn, die automatische Verlängerung wird spätestens 24 Stunden vor dem Ende der aktiven Laufzeit deaktiviert. Das Konto wird in den 24 Stunden vor dem Ende der aktiven Laufzeit belastet, wobei die Kosten der Verlängerung angegeben werden. Abonnements können durch den Nutzer verwaltet werden. Die automatische Verlängerung lässt sich nach dem Kauf in den Kontoeinstellungen deaktivieren. Nicht genutzte Restlaufzeiten einer kostenlosen Testphase verfallen, wenn der Nutzer ein Abonnement für das entsprechende Produkt erwirbt, sofern zutreffend.",
    "Licensing issue": "Lizenzierungsproblem",
    "Light": "Hell",
    "Light theme": "Helles Design",
    "Log in": "Anmelden",
    "Log out": "Abmelden",
    "Logging in...": "Anmeldung läuft ...",
    "Monthly:": "Monatlich:",
    "My Account": "Mein Konto",
    "New to Avira?": "Sind Sie neu bei Avira?",
    "Next": "Weiter",
    "No Wi-Fi network saved yet for automatic VPN connection. Once saved, they will automatically appear here and can be configured.": "Es wurde noch kein WLAN für die automatische VPN-Verbindung gespeichert. Sobald Sie ein WLAN gespeichert haben, erscheint es automatisch hier und kann konfiguriert werden.",
    "No network available.": "Kein Netzwerk verfügbar",
    "No purchase found. Nothing to restore.": "Kein Kauf gefunden. Nichts zu sichern.",
    "Not Now": "Nicht jetzt",
    "Not now": "Nicht jetzt",
    "OS information": "Informationen zum Betriebssystem",
    "Okay": "OK",
    "On all my devices": "Auf allen meinen Geräten",
    "On my Mac": "Auf meinem Mac",
    "On my PC": "Auf meinem PC",
    "Oops. Sorry, there was a error in the authentication process. Try again later or contact Support.": "Ups! Während der Authentifizierung ist ein Fehler aufgetreten. Versuchen Sie es später erneut oder kontaktieren Sie den Support.",
    "Oops. Sorry, there was a error in the registration process. Try again later or contact Support.": "Ups! Während der Registrierung ist ein Fehler aufgetreten. Versuchen Sie es später erneut oder kontaktieren Sie den Support.",
    "Oops. Sorry, this email address is already registered with another account.": "Ups! Die E-Mail-Adresse wurde schon für ein anderes Konto registriert.",
    "Other": "Anderes",
    "Password": "Passwort",
    "Please register if you want to purchase Phantom VPN Pro on all your devices": "Registrieren Sie sich, wenn Sie Phantom VPN Pro für alle Ihre Geräte kaufen möchten.",
    "Privacy is important to us": "Ihre Privatsphäre ist uns wichtig",
    "Privacy policy": "Datenschutz-Hinweise",
    "Purchasing...": "Kauf wird bearbeitet …",
    "Quit": "Beenden",
    "Rate 5 stars": "5 Sterne vergeben",
    "Rate Avira Phantom VPN": "Avira Phantom VPN bewerten",
    "Register": "Registrieren",
    "Register for an Avira account<br>and get your 30 day free trial": "Jetzt für ein Avira Konto registrieren<br>und gratis die 30-tägige Testversion nutzen",
    "Registering...": "Registrierung läuft ...",
    "Renew now": "Jetzt verlängern",
    "Resend email": "E-Mail erneut senden",
    "Restore purchase": "Einkauf wiederherstellen",
    "Restoring...": "Wiederherstellung läuft …",
    "Secure my connection": "Meine Verbindung sichern",
    "Securing your connection": "Sichern Ihrer Verbindung",
    "Select virtual location": "Virtuellen Standort auswählen",
    "Send": "Senden",
    "Send diagnostic data": "Diagnosedaten senden",
    "Send feedback": "Feedback senden",
    "Send report": "Bericht erstellen",
    "Sending data...": "Daten werden gesendet ...",
    "Settings": "Einstellungen",
    "Special Offer": "Sonderangebot",
    "Speed issue": "Geschwindigkeitsproblem",
    "Subscription terms": "Abonnementbedingungen",
    "Tap Adapter not present or disabled.": "TAP Adapter steht nicht zur Verfügung oder ist deaktiviert.",
    "Technical data": "Technische Daten",
    "Tell us what you think. Your feedback helps us improve.": "Sagen Sie uns Ihre Meinung. Helfen Sie uns besser zu werden.",
    "Terms and conditions": "Geschäftsbedingungen",
    "Test Pro for free": "Pro kostenlos testen",
    "Thank you for helping us to improve the product by providing valuable information.": "Vielen Dank für Ihr Feedback! Sie helfen uns damit, unser Produkt zu verbessern.",
    "The amount of data that you consume. We do this to help calculate the costs of providing our VPN infrastructure": "Die Datenmenge, die Sie verbrauchen. So können wir unsere Kosten kalkulieren.",
    "The app's color theme will use the operating system setting unless you choose a different theme.": "Das Farb-Design der App richtet sich nach den Betriebssystem-Einstellungen, sofern Sie kein anderes Design gewählt haben.",
    "The app's color theme will use the operating system setting.": "Das Farb-Design der App richtet sich nach den Betriebssystem-Einstellungen.",
    "The connection is not private": "Die Verbindung ist nicht sicher",
    "The connection is private": "Die Verbindung ist sicher",
    "The following technical data will be collected and sent to Avira for troubleshooting:": "Folgende Daten werden erfasst und zur Problembehebung an Avira gesendet:",
    "The report has been sent.": "Der Bericht wurde gesendet.",
    "The selected location is invalid. Please check the network connection.": "Der ausgewählte Standort ist ungültig. Bitte überprüfen Sie die Netzwerkverbindung.",
    "Three-Month free trial, then only <span>{0}</span> monthly": "Kostenlose dreimonatige Testversion, danach nur <span>{0}</span>/Monat",
    "To encrypt your internet connection, Phantom VPN uses the neagent system service. It needs your permission to access the Keychain.": "Zur Verschlüsselung Ihrer Internetverbindung nutzt Phantom VPN den Systemdienst Neagent. Für dessen Einrichtung ist Ihre Erlaubnis erforderlich.",
    "To try the best experience we will give you unlimited data volume for 30 days for free.": "Zum Ausprobieren unserer besten Lösung erhalten Sie 30 Tage lang ein unbegrenztes Datenvolumen – kostenfrei.",
    "Traffic limit reached": "Die Obergrenze des Datenverkehrs ist erreicht",
    "Trial": "Testversion",
    "Unknown": "Unbekannt",
    "Unknown error.": "Unbekannter Fehler.",
    "Unlimited": "Unbegrenzt",
    "Unlimited traffic": "Unbegrenzter Datenverkehr",
    "Unlimited traffic on: Windows, macOS, iOS, Android, Google Chrome": "Unbegrenzter Datenverkehr auf: Windows, macOS, iOS, Android, Google Chrome",
    "Use fastest protocol when available": "Schnellstes Protokoll verwenden, wenn verfügbar",
    "Use system settings": "Systemeinstellungen verwenden",
    "VPN connection details": "VPN-Verbindungsdetails",
    "Verification code": "Verifizierungscode",
    "Virtual location set to <a> {0} </a>": "Virtueller Standort: <a> {0} </a>",
    "Virtual location: {0}": "Virtueller Standort: {0}",
    "We do not gather any personal data, only the technical details. All information you provide will be deleted after the troubleshooting process.": "Wir erfassen keinerlei persönliche Daten, nur technische Details. Alle von Ihnen übermittelten Informationen werden nach Behebung des Problems gelöscht.",
    "We sent a verification code to your phone number": "Wir haben einen Verifizierungscode an Ihre Telefonnummer gesendet.",
    "We want to be fully transparent about the data you agree to share with us. To provide you a smooth VPN experience we need a minimum set of data:": "Wir möchten Ihnen gegenüber transparent sein über die Daten, die Sie mit uns teilen. Um Ihnen eine optimale VPN-Erfahrung anzubieten, benötigen wir ein Minimum an Daten:",
    "What kind of issue do you want to report?": "Welches Problem möchten Sie melden?",
    "Whether you are a free or a paid user. It's important for our communications to be able to differentiate the two": "Ob Sie die kostenlose oder die Bezahlversion nutzen. Diese Unterscheidung ist wichtig, dass wir unsere Kundenkommunikation anpassen können.",
    "Yearly:": "Jährlich:",
    "You will be connected in <a>{{secondsRemaining}}</a> seconds": "Sie werden in <a>{{secondsRemaining}}</a> Sekunden verbunden",
    "You will be disconnected in {0} seconds.": "Ihre Verbindung wird in {0} Sekunden getrennt.",
    "Your connection is secure": "Ihre Verbindung ist sicher",
    "Your connection is secure.": "Ihre Verbindung ist sicher",
    "Your connection is unsecure": "Ihre Verbindung ist unsicher",
    "Your data usage": "Ihr Datenverbrauch",
    "Your internet connection was protected from prying eyes. Is this worth 5 stars?": "Ihre Internetverbindung wurde vor neugierigen Blicken geschützt. Ist das nicht 5 Sterne wert?",
    "Your license will expire soon.<br/>Remaining days: {0}": "Ihre Lizenz läuft demnächst ab.<br/>Noch {0} Tage",
    "Your password must contain at least 8 characters, one digit, and one uppercase letter.": "Ihr Passwort muss mindestens 8 Zeichen, eine Zahl und einen Großbuchstaben enthalten.",
    "default": "Standardeinstellung",
    "https://www.research.net/r/HLV5Q2H?": "https://www.research.net/r/HLV5Q2H?",
    "https://www.research.net/r/J5H53HC?": "https://www.research.net/r/J5H53HC?",
    "of traffic used.": "Ihres Datenverkehrs verbraucht.",
    "or get 500 MB if you <a> register </a>": "oder sichern Sie sich zusätzlich 500 MB, indem Sie sich <a> registrieren </a>",
    "remaining this month.": "verbleiben für diesen Monat.",
    "{0} out of {1} daily secured traffic": "{0} von {1} täglich gesichertem Datenverkehr",
    "{0} out of {1} monthly secured traffic": "{0} von {1} monatlich gesichertem Datenverkehr",
    "{0} out of {1} weekly secured traffic": "{0} von {1} wöchentlich gesichertem Datenverkehr",
    "{0} secured traffic": "{0} gesicherter Datenverkehr",
    "%": " %",
    "{0} used": "{0} verwendet"
  });
  /* jshint +W100 */
}]);

},{}],60:[function(require,module,exports){
"use strict";

angular.module('LauncherApp').run(['gettextCatalog', function (gettextCatalog) {
  /* jshint -W100 */
  gettextCatalog.setStrings('en_US', {
    "0 bytes": "0 bytes",
    "<span class=\"privacyDescription__text__bold\">We don't log your online activity or any information that can link you to any action</span>, such as downloading a file, or visiting a particular website.": "<span class=\"privacyDescription__text__bold\">We don't log your online activity or any information that can link you to any action</span>, such as downloading a file, or visiting a particular website.",
    "A new driver has been installed, please restart your computer to complete the installation": "Please restart your computer before securing your connection.",
    "About": "About",
    "Access": "Continue",
    "Agree and continue": "Agree and continue",
    "All free slots are currently taken.": "No slots free currently.",
    "Already have an account?": "Already have an account?",
    "Auto-connect VPN": "Auto-connect VPN",
    "Auto-connect VPN for Wi-Fi networks": "Auto-connect VPN for Wi-Fi networks",
    "Automatically secure untrusted Wi-Fi networks": "Automatically secure untrusted Wi-Fi networks",
    "Avira Phantom VPN Pro released!": "Avira Phantom VPN Pro has arrived!",
    "Back": "Back",
    "Block all internet traffic if VPN connection drops": "Block all internet traffic if VPN connection drops",
    "Block malicious sites and content": "Block malicious sites and content",
    "Buy": "Buy",
    "Buy unlimited traffic": "Buy unlimited traffic",
    "By proceeding, you are accepting the <a href=\"#\" ng-click=\"openEula()\">End User License Agreement (EULA)</a>, and the <a href=\"#\" ng-click=\"openTermsAndConditions()\">Terms and Conditions</a>. Avira is fulfilling its duties to provide information in accordance with Articles 13 and 14 of the General Data Protection Regulation (GDPR) with the contents of the Privacy Policy and access thereto. You can find our Privacy Policy here: <a href=\"#\" ng-click=\"openPrivacyAndPolicy()\">{{getPrivacyPolicyLink()}}</a>": "By proceeding, you are accepting the <a href=\"#\" ng-click=\"openEula()\">End User License Agreement (EULA)</a> and the <a href=\"#\" ng-click=\"openTermsAndConditions()\">Terms and Conditions</a>. Avira is fulfilling its duties to provide information in accordance with Articles 13 and 14 of the General Data Protection Regulation (GDPR) with the contents of the Privacy Policy and access thereto. You can find our Privacy Policy here: <a href=\"#\" ng-click=\"openPrivacyAndPolicy()\">{{getPrivacyPolicyLink()}}</a>",
    "Cancel": "Cancel",
    "Cannot resolve host address.": "Cannot resolve host address.",
    "Check your inbox to confirm it's you. Make sure to look into your spam and junk folders as well.": "Check your inbox to confirm it's you. Make sure to look into your spam and junk folders as well.",
    "Code": "Code",
    "Collect diagnostic data": "Send",
    "Connect": "Connect",
    "Connecting": "Connecting",
    "Connecting...": "Connecting...",
    "Connection to server lost.": "Connection to server lost.",
    "Continue": "Continue",
    "Disconnect": "Disconnect",
    "Disconnecting": "Disconnecting",
    "Disconnecting...": "Disconnecting...",
    "Don't have an account yet?": "No account?",
    "Don't send": "Don't send",
    "Don't want to wait? Upgrade now": "Why wait? Upgrade now",
    "Email address": "Email address",
    "Email confirmation": "Email confirmation",
    "Email sent": "Email sent",
    "Enjoy your unlimited data volume.<br/>Remaining days: {0}": "Enjoy your unlimited data volume.<br/>Remaining days: {0}",
    "Enter a valid email address first": "Enter a valid email address",
    "Enter a valid password": "Enter a valid password",
    "Enter a valid verification code.": "Enter a valid verification code.",
    "Establishing connection...": "Establishing connection...",
    "Exit": "Exit",
    "Failed to connect to the service.": "Failed to connect to the service.",
    "Failed to connect using UDP protocol. Retrying with TCP protocol.": "Failed to connect using UDP protocol. Retrying with TCP protocol.",
    "Failed to establish the VPN connection. Please try again.": "Unable to connect. Please try again.",
    "Failed to purchase. Please try again later.": "Purchase failed. Please try again later.",
    "Fatal error.": "Fatal error.",
    "Fill in this field.": "Fill in this field.",
    "For security reasons your account has been blocked. Please access Avira Connect to unblock it.": "Your account has been locked. You can unlock it by simply accessing Avira Connect.",
    "Forgot your password?": "Forgot your password?",
    "Get 3 Months Free": "Get Pro for 3 months",
    "Get Pro": "Get Pro",
    "Get all the protection you need.": "Get all the protection you need.",
    "Get for this occasion your three-month free trial. Thank you very much for helping us in beta stage by using and testing our product.": "Get your three month free special offer. Thank you for trying our product and helping us improving it.",
    "Get support": "Get support",
    "Get unlimited data volume now.": "Get unlimited access now.",
    "Help Avira improve its products and services by automatically sending daily anonymous diagnostic and usage data.": "Help Avira improve our products and services by automatically sending daily anonymous diagnostic and usage data.",
    "Help us improve": "Help us improve",
    "I entered a wrong email": "I entered a wrong email address",
    "In case of technical issues you can collect diagnostic data and send a report to the developers.": "Having technical issues? We're here for you. Send us a report and we'll do the rest.",
    "Invalid credentials": "Invalid credentials",
    "Launch at system start": "Launch at system start",
    "Length of subscription: 1 month/1 year<br><br> \n                    Price of subscription: 4,95€ per month (Mac only) – 7,95€ per month (all devices) – 59,95€ per year (all devices).<br><br>\n                    \n                    Payment will be charged to iTunes Account at confirmation of purchase. Subscription automatically renews unless auto-renew is turned off at least 24-hours before the end of the current period. Account will be charged for renewal within 24-hours prior to the end of the current period, and identify the cost of the renewal. Subscriptions may be managed by the user and auto-renewal may be turned off by going to the user's Account Settings after purchase. Any unused portion of a free trial period, if offered, will be forfeited when the user purchases a subscription to that publication, where applicable.": "Length of subscription: 1 month/1 year - Price of subscription: 4,95 € per month (Mac only) - 7,95 EUR per month (all devices) - 59,95 EUR per year (all devices).<br><br> Payment will be charged to iTunes Account at confirmation of purchase. Subscription automatically renews unless auto-renew is turned off at least 24-hours before the end of the current period. Account will be charged for renewal within 24-hours prior to the end of the current period, and identify the cost of the renewal. Subscriptions may be managed by the user and auto-renewal may be turned off by going to the user's Account Settings after purchase. Any unused portion of a free trial period, if offered, will be forfeited when the user purchases a subscription to that publication, where applicable.",
    "Log in": "Log in",
    "Logging in...": "Logging in...",
    "Monthly:": "Monthly:",
    "My Account": "My Account",
    "New to Avira?": "Are you new to Avira?",
    "No Wi-Fi network saved yet for automatic VPN connection. Once saved, they will automatically appear here and can be configured.": "No Wi-Fi network saved yet for automatic VPN connection. Once saved, they will automatically appear here and can be configured.",
    "No network available.": "No network available.",
    "Not Now": "Not now",
    "Not now": "Not now",
    "On all my devices": "On all my devices",
    "On my Mac": "On my Mac",
    "On my PC": "On my PC",
    "Oops. Sorry, there was a error in the authentication process. Try again later or contact Support.": "Oops. Sorry, there was an error in the authentication process. Try again later or contact Support.",
    "Oops. Sorry, there was a error in the registration process. Try again later or contact Support.": "Oops. Sorry, there was an error in the registration process. Try again later or contact Support.",
    "Oops. Sorry, this email address is already registered with another account.": "Oops. Sorry, this email address is already registered with another account.",
    "Password": "Password",
    "Please register if you want to purchase Phantom VPN Pro on all your devices": "Register to purchase Phantom VPN Pro for all your devices.",
    "Privacy is important to us": "Privacy is important to us",
    "Privacy policy": "Privacy Policy",
    "Purchasing...": "Purchasing...",
    "Quit": "Quit",
    "Register": "Register",
    "Register for an Avira account<br>and get your 30 day free trial": "Register for an Avira account<br>and get your 30 day free trial",
    "Registering...": "Registering...",
    "Renew now": "Renew now",
    "Resend email": "Resend email",
    "Restore purchase": "Restore purchase",
    "Restoring...": "Restoring...",
    "Secure my connection": "Secure my connection",
    "Select virtual location": "Select virtual location",
    "Send": "Send",
    "Send diagnostic data": "Send diagnostic data",
    "Send feedback": "Send feedback",
    "Send report": "Create report",
    "Settings": "Settings",
    "Special Offer": "Special offer",
    "Subscription terms": "Subscription terms",
    "Tap Adapter not present or disabled.": "Tap Adapter not present or disabled.",
    "Tell us what you think. Your feedback helps us improve.": "Tell us what you think. Your feedback helps us improve.",
    "Terms and conditions": "Terms and conditions",
    "The amount of data that you consume. We do this to help calculate the costs of providing our VPN infrastructure": "The amount of data that you consume. We do this to help calculate the costs of providing our VPN infrastructure.",
    "The connection is not private": "The connection is not private",
    "The connection is private": "The connection is private",
    "The selected location is invalid. Please check the network connection.": "The selected location is invalid. Please check the network connection.",
    "Three-Month free trial, then only <span>{0}</span> monthly": "Three-month free trial, then only <span>{0}</span> monthly",
    "To try the best experience we will give you unlimited data volume for 30 days for free.": "To try the best experience, you will get unlimited data volume free for 30 days.",
    "Traffic limit reached": "Traffic limit reached",
    "Trial": "Trial",
    "Unknown": "Unknown",
    "Unknown error.": "Unknown error.",
    "Unlimited": "Unlimited",
    "Unlimited traffic": "Unlimited traffic",
    "Unlimited traffic on: Windows, macOS, iOS, Android, Google Chrome": "Unlimited traffic on: Windows, macOS, iOS, Android, Google Chrome",
    "Use fastest protocol when available": "Use fastest protocol when available",
    "Verification code": "Verification code",
    "Virtual location set to <a> {0} </a>": "Virtual location: <a> {0} </a>",
    "Virtual location: {0}": "Virtual location: {0}",
    "We sent a verification code to your phone number": "We sent a verification code to your phone number.",
    "We want to be fully transparent about the data you agree to share with us. To provide you a smooth VPN experience we need a minimum set of data:": "We want to be fully transparent about the data you agree to share with us. To provide you a smooth VPN experience we need a minimum set of data:",
    "Whether you are a free or a paid user. It's important for our communications to be able to differentiate the two": "Whether you are a free or a paid user. It is important for our communications to be able to differentiate the two.",
    "Yearly:": "Yearly:",
    "You will be disconnected in {0} seconds.": "You will be disconnected in {0} seconds.",
    "Your connection is secure": "Your connection is secure",
    "Your connection is secure.": "Your connection is secure",
    "Your connection is unsecure": "Your connection is not secure",
    "Your license will expire soon.<br/>Remaining days: {0}": "Your license will expire soon.<br/>Remaining days: {0}",
    "Your password must contain at least 8 characters, one digit, and one uppercase letter.": "Your password must contain at least 8 characters, one digit, and one uppercase letter.",
    "https://www.research.net/r/HLV5Q2H?": "https://www.research.net/r/HLV5Q2H?",
    "https://www.research.net/r/J5H53HC?": "https://www.research.net/r/J5H53HC?",
    "or get 500 MB if you <a> register </a>": "or get 500 MB if you <a> register </a>",
    "{0} out of {1} daily secured traffic": "{0} out of {1} daily secured traffic",
    "{0} out of {1} monthly secured traffic": "{0} out of {1} monthly secured traffic",
    "{0} out of {1} weekly secured traffic": "{0} out of {1} weekly secured traffic",
    "{0} secured traffic": "{0} secured traffic",
    "%": "%",
    "{0} used": "{0} used"
  });
  /* jshint +W100 */
}]);

},{}],61:[function(require,module,exports){
"use strict";

angular.module('LauncherApp').run(['gettextCatalog', function (gettextCatalog) {
  /* jshint -W100 */
  gettextCatalog.setStrings('es_ES', {
    "0 bytes": "0 bytes",
    "<span class=\"privacyDescription__text__bold\">We don't log your online activity or any information that can link you to any action</span>, such as downloading a file, or visiting a particular website.": "<span class=\"privacyDescription__text__bold\">No registramos su actividad en línea ni información que pueda vincularle con una acción</span> como descargar un archivo o visitar un sitio web determinado.",
    "A new driver has been installed, please restart your computer to complete the installation": "Reinicie el ordenador antes de proteger su conexión.",
    "About": "Acerca de",
    "Access": "Continuar",
    "Account details": "Información de la cuenta",
    "Agree and continue": "Aceptar y continuar",
    "All free slots are currently taken.": "No slots free currently.",
    "Already have an account?": "¿Ya tiene una cuenta?",
    "Always Allow": "Permitir siempre",
    "Auto-connect VPN": "Conexión automática a VPN",
    "Auto-connect VPN for Wi-Fi networks": "Conexión automática a VPN para redes wifi",
    "Automatically secure untrusted Wi-Fi networks": "Proteger automáticamente redes Wi-Fi no fiables",
    "Avira Phantom VPN Pro released!": "¡Avira Phantom VPN Pro ha llegado!",
    "Back": "Atrás",
    "Block all internet traffic if VPN connection drops": "Bloquear todo el tráfico de Internet si falla la conexión VPN",
    "Block intrusive ads": "Bloquear anuncios intrusivos",
    "Block malicious sites and content": "Bloquear sitios y contenido maliciosos",
    "Buy": "Comprar",
    "Buy unlimited traffic": "Comprar tráfico ilimitado",
    "By proceeding, you are accepting the <a href=\"#\" ng-click=\"openEula()\">End User License Agreement (EULA)</a>, and the <a href=\"#\" ng-click=\"openTermsAndConditions()\">Terms and Conditions</a>. Avira is fulfilling its duties to provide information in accordance with Articles 13 and 14 of the General Data Protection Regulation (GDPR) with the contents of the Privacy Policy and access thereto. You can find our Privacy Policy here: <a href=\"#\" ng-click=\"openPrivacyAndPolicy()\">{{getPrivacyPolicyLink()}}</a>": "Al continuar, acepta el <a href=\"#\" ng-click=\"openEula()\">Acuerdo de licencia de usuario final (ALUF)</a> y los <a href=\"#\" ng-click=\"openTermsAndConditions()\">Términos y condiciones</a>. Avira cumple sus obligaciones de ofrecer información de conformidad con los artículos 13 y 14 del Reglamento General de Protección de Datos (RGPD) en lo referente al contenido de la política de privacidad y el acceso a los mismos. Puede leer nuestra política de privacidad aquí: <a href=\"#\" ng-click=\"openPrivacyAndPolicy()\">{{getPrivacyPolicyLink()}}</a>",
    "Cancel": "Cancelar",
    "Cannot resolve host address.": "No puede solucionarse la dirección host.",
    "Change later in Settings": "Cambiarlo después en Ajustes",
    "Check your inbox to confirm it's you. Make sure to look into your spam and junk folders as well.": "Compruebe su bandeja de entrada para confirmar que es usted. Asegúrese de consultar su correo no deseado y archivos basura.",
    "Choose your color theme": "Elige tu tema de color",
    "Code": "Código",
    "Collect diagnostic data": "Enviar",
    "Connect": "Conectar",
    "Connecting": "Conectando",
    "Connecting...": "Conectando...",
    "Connection to server lost.": "Se ha perdido la conexión al servidor.",
    "Continue": "Continuar",
    "Dark": "Oscuro",
    "Dark theme": "Tema oscuro",
    "Disconnect": "Desconectar",
    "Disconnecting": "Desconectando",
    "Disconnecting...": "Desconectando...",
    "Display settings": "Ajustes de pantalla",
    "Don't have an account yet?": "¿No tienes cuenta?",
    "Don't send": "No enviar",
    "Don't show again": "No volver a mostrar",
    "Don't show this again": "No volver a mostrarlo",
    "Don't want to wait? Upgrade now": "Why wait? Upgrade now",
    "Done": "Hecho",
    "Email address": "Dirección de correo electrónico",
    "Email confirmation": "Confirmación de correo",
    "Email sent": "Correo enviado",
    "Enjoy your unlimited data volume.<br/>Remaining days: {0}": "Disfrute de su volumen de datos ilimitado.<br/>Días restantes: {0}",
    "Enter a valid email address first": "Introduzca una dirección de correo electrónico válida",
    "Enter a valid password": "Introduzca una contraseña válida",
    "Enter a valid verification code.": "Introduzca un código de verificación válido.",
    "Enter your Mac password and choose <span class=\"privacyDescription__text__bold\">Always allow</span> in the next window to continue.": "Introduzca su contraseña Mac en la siguiente ventana, seleccione <span class=\"privacyDescription__text__bold\">Permitir siempre</span> para continuar.",
    "Establishing connection...": "Estableciendo conexión...",
    "Exit": "Salir",
    "Failed to connect to the service.": "No se ha podido conectar con el servicio.",
    "Failed to connect using UDP protocol. Retrying with TCP protocol.": "No se ha podido conectar mediante el uso del protocolo UDP. Inténtalo de nuevo con el protocolo TCP.",
    "Failed to establish the VPN connection. Please try again.": "No se ha podido conectar. Vuelve a intentarlo.",
    "Failed to purchase. Please try again later.": "Pedido incorrecto. Reinténtelo más tarde.",
    "Fatal error.": "Error fatal.",
    "Fill in this field.": "Rellene este campo.",
    "For security reasons your account has been blocked. Please access Avira Connect to unblock it.": "Su cuenta se ha bloqueado. Puede desbloquearla con facilidad mediante el acceso a Avira Connect.",
    "Forgot your password?": "¿Has olvidado tu contraseña?",
    "Get 3 Months Free": "Obtenga Pro durante 3 meses",
    "Get Pro": "Obtener Pro",
    "Get all the protection you need.": "Obtén toda la protección que necesitas.",
    "Get for this occasion your three-month free trial. Thank you very much for helping us in beta stage by using and testing our product.": "Obtenga su oferta especial gratuita de tres meses. Gracias por probar nuestro producto y ayudarnos a mejorarlo.",
    "Get started": "Comenzar",
    "Get support": "Obtener soporte",
    "Get unlimited data volume now.": "Disfruta ya de acceso ilimitado.",
    "Help Avira improve its products and services by automatically sending daily anonymous diagnostic and usage data.": "Ayude a Avira a mejorar nuestros productos y servicios mediante el envío diario automático de datos de diagnóstico y uso anónimos.",
    "Help us improve": "Ayúdenos a mejorar",
    "I entered a wrong email": "Introduje la dirección de correo incorrecta",
    "IPSec traffic is blocked. Please contact your network administrator.": "Tráfico IPSec bloqueado. Contacte con su administrador de red.",
    "In case of technical issues you can collect diagnostic data and send a report to the developers.": "Having technical issues? We're here for you. Send us a report and we'll do the rest.",
    "Invalid credentials": "Credenciales no válidas",
    "Launch at system start": "Ejecutar al inicio del sistema",
    "Length of subscription: 1 month/1 year<br><br> \n                    Price of subscription: 4,95€ per month (Mac only) – 7,95€ per month (all devices) – 59,95€ per year (all devices).<br><br>\n                    \n                    Payment will be charged to iTunes Account at confirmation of purchase. Subscription automatically renews unless auto-renew is turned off at least 24-hours before the end of the current period. Account will be charged for renewal within 24-hours prior to the end of the current period, and identify the cost of the renewal. Subscriptions may be managed by the user and auto-renewal may be turned off by going to the user's Account Settings after purchase. Any unused portion of a free trial period, if offered, will be forfeited when the user purchases a subscription to that publication, where applicable.": "Duración de la suscripción: 1 mes/1 año, Precio de la suscripción: 4,95 € al mes (solo Mac), 7,95 € al mes (todos los dispositivos), 59,95 € al año (todos los dispositivos). El pago se cargará en la cuenta iTunes en el momento de la confirmación del pedido. La suscripción se renueva de forma automática a menos que se desactive la renovación automática como mínimo 24 horas antes de que finalice el periodo actual. El pago se cargará en la cuenta para la renovación en el plazo de 24 horas antes de que finalice el periodo actual y se identificará el precio de la renovación. El usuario podrá gestionar las suscripciones y la renovación automática podrá desactivarse a través de la configuración de cuenta del usuario tras el pedido. Toda parte no utilizada de un periodo de prueba gratuito, en caso de que se haya ofrecido, se perderá en el momento en el que el usuario adquiera una suscripción para dicha publicación, siempre que sea aplicable.",
    "Light": "Claro",
    "Light theme": "Tema claro",
    "Log in": "Iniciar sesión",
    "Log out": "Cerrar sesión",
    "Logging in...": "Iniciando sesión...",
    "Monthly:": "Mensual:",
    "My Account": "Mi cuenta",
    "New to Avira?": "¿Eres nuevo en Avira?",
    "No Wi-Fi network saved yet for automatic VPN connection. Once saved, they will automatically appear here and can be configured.": "No hay ninguna red wifi guardada en la que VPN se conecte de forma automática. Aquí aparecerán, una vez guardadas, y podrás configurarlas.",
    "No network available.": "Ninguna red disponible",
    "No purchase found. Nothing to restore.": "Ningún pedido encontrado. Nada para restaurar.",
    "Not Now": "Ahora no",
    "Not now": "Ahora no",
    "Okay": "Aceptar",
    "On all my devices": "En todos mis dispositivos",
    "On my Mac": "En mi Mac",
    "On my PC": "En mi equipo",
    "Oops. Sorry, there was a error in the authentication process. Try again later or contact Support.": "¡Vaya! Lo sentimos, se ha producido un error en el proceso de autenticación. Inténtelo de nuevo más tarde o póngase en contacto con el soporte.",
    "Oops. Sorry, there was a error in the registration process. Try again later or contact Support.": "¡Vaya! Lo sentimos, se ha producido un error en el proceso de registro. Inténtelo de nuevo más tarde o póngase en contacto con el soporte.",
    "Oops. Sorry, this email address is already registered with another account.": "¡Vaya! Lo sentimos, esta dirección de correo ya se ha registrado con otra cuenta.",
    "Password": "Contraseña",
    "Please register if you want to purchase Phantom VPN Pro on all your devices": "Regístrese para adquirir Phantom VPN Pro para todos sus dispositivos.",
    "Privacy is important to us": "La privacidad nos importa",
    "Privacy policy": "Política de privacidad",
    "Purchasing...": "Pedido en curso...",
    "Quit": "Salir",
    "Rate 5 stars": "Valoración de 5 estrellas",
    "Rate Avira Phantom VPN": "Valore Avira Phantom VPN",
    "Register": "Registrarse",
    "Register for an Avira account<br>and get your 30 day free trial": "Regístrese para crear una cuenta Avira<br>y obtenga su prueba gratuita de 30 días",
    "Registering...": "Registrando...",
    "Renew now": "Renovar ahora",
    "Resend email": "Reenviar correo",
    "Restore purchase": "Restaurar compra",
    "Restoring...": "Restauración en curso...",
    "Secure my connection": "Proteger mi conexión",
    "Securing your connection": "Protección de la conexión",
    "Select virtual location": "Selecciona una ubicación virtual",
    "Send": "Enviar",
    "Send diagnostic data": "Enviar datos de diagnóstico",
    "Send feedback": "Enviar feedback",
    "Send report": "Create report",
    "Settings": "Configuración",
    "Special Offer": "Oferta especial",
    "Subscription terms": "Condiciones de suscripción",
    "Tap Adapter not present or disabled.": "Adaptador TAP inexistente o desactivado",
    "Tell us what you think. Your feedback helps us improve.": "Cuéntenos lo que piensa. Su opinión nos ayuda a mejorar.",
    "Terms and conditions": "Términos y Condiciones",
    "Test Pro for free": "Probar Pro gratis",
    "The amount of data that you consume. We do this to help calculate the costs of providing our VPN infrastructure": "La cantidad de datos que consume. Necesitamos saberlo para calcular los costes de ofrecer la infraestructura de nuestra VPN.",
    "The app's color theme will use the operating system setting unless you choose a different theme.": "El tema de color de la aplicación usará la configuración del sistema operativo, a menos que tú elijas uno diferente.",
    "The app's color theme will use the operating system setting.": "El tema de color de la aplicación usará la configuración del sistema operativo.",
    "The connection is not private": "La conexión no es confidencial",
    "The connection is private": "La conexión es confidencial",
    "The selected location is invalid. Please check the network connection.": "La ubicación seleccionada no es válida. Comprueba tu conexión de red.",
    "Three-Month free trial, then only <span>{0}</span> monthly": "Prueba gratuita de tres meses, luego solo <span>{0}</span> al mes",
    "To encrypt your internet connection, Phantom VPN uses the neagent system service. It needs your permission to access the Keychain.": "Para cifrar su conexión a Internet, Phantom VPN utiliza el servicio del sistema neagent. Necesitamos su permiso para configurarlo.",
    "To try the best experience we will give you unlimited data volume for 30 days for free.": "Para probar una experiencia inmejorable le ofreceremos un volumen de datos ilimitado gratuito durante 30 días.",
    "Traffic limit reached": "Límite de tráfico alcanzado",
    "Trial": "Versión de prueba",
    "Unknown": "Desconocido",
    "Unknown error.": "Error desconocido.",
    "Unlimited": "Ilimitado",
    "Unlimited traffic": "Tráfico ilimitado",
    "Unlimited traffic on: Windows, macOS, iOS, Android, Google Chrome": "Tráfico ilimitado en: Windows, macOS, iOS, Android, Google Chrome",
    "Use fastest protocol when available": "Utilizar el protocolo más rápido si está disponible",
    "Use system settings": "Usar configuración del sistema",
    "Verification code": "Código de verificación",
    "Virtual location set to <a> {0} </a>": "Ubicación virtual: <a> {0} </a>",
    "Virtual location: {0}": "Ubicación virtual: {0}",
    "We sent a verification code to your phone number": "Hemos enviado un código de verificación a su número de teléfono.",
    "We want to be fully transparent about the data you agree to share with us. To provide you a smooth VPN experience we need a minimum set of data:": "Queremos ser del todo transparentes en cuanto a los datos que accede a compartir con nosotros. Para ofrecerle una buena experiencia VPN necesitamos un conjunto de datos mínimo:",
    "Whether you are a free or a paid user. It's important for our communications to be able to differentiate the two": "Tanto si es usuario del producto gratuito o de pago. Para nuestras comunicaciones es importante saber a qué grupo pertenece.",
    "Yearly:": "Anual:",
    "You will be disconnected in {0} seconds.": "Desconexión en {0} segundos.",
    "Your connection is secure": "La conexión es segura",
    "Your connection is secure.": "La conexión es segura",
    "Your connection is unsecure": "Tu conexión no es segura",
    "Your internet connection was protected from prying eyes. Is this worth 5 stars?": "Tu conexión a Internet está protegida contra miradas indiscretas. ¿Se merece 5 estrellas?",
    "Your license will expire soon.<br/>Remaining days: {0}": "Tu licencia está a punto de caducar.<br/>Días que faltan: {0}",
    "Your password must contain at least 8 characters, one digit, and one uppercase letter.": "Tu contraseña debe contener como mínimo 8 caracteres, un dígito y una letra en mayúsculas.",
    "default": "predeterminado",
    "https://www.research.net/r/HLV5Q2H?": "https://www.research.net/r/HLV5Q2H?",
    "https://www.research.net/r/J5H53HC?": "https://www.research.net/r/J5H53HC?",
    "or get 500 MB if you <a> register </a>": "o bien obtén 500 MB <a> registrándote </a>",
    "{0} out of {1} daily secured traffic": "{0} de {1} del tráfico diario protegido",
    "{0} out of {1} monthly secured traffic": "{0} de {1} del tráfico mensual protegido",
    "{0} out of {1} weekly secured traffic": "{0} de {1} del tráfico semanal protegido",
    "{0} secured traffic": "{0} de tráfico protegido",
    "%": "%",
    "{0} used": "{0} utilizado"
  });
  /* jshint +W100 */
}]);

},{}],62:[function(require,module,exports){
"use strict";

angular.module('LauncherApp').run(['gettextCatalog', function (gettextCatalog) {
  /* jshint -W100 */
  gettextCatalog.setStrings('fr_FR', {
    "0 bytes": "0 octet",
    "<span class=\"privacyDescription__text__bold\">We don't log your online activity or any information that can link you to any action</span>, such as downloading a file, or visiting a particular website.": "<span class=\"privacyDescription__text__bold\">Nous ne consignons pas vos activités en ligne ni aucune information permettant de vous relier à une action quelconque</span>, à savoir, le téléchargement d’un fichier ou la consultation d’un site Internet particulier.",
    "A new driver has been installed, please restart your computer to complete the installation": "Veuillez redémarrer l’ordinateur avant de sécuriser la connexion.",
    "About": "À propos",
    "Access": "Continuer",
    "Account details": "Informations sur votre compte",
    "Agree and continue": "Accepter et continuer",
    "All free slots are currently taken.": "No slots free currently.",
    "Already have an account?": "Vous avez déjà un compte?",
    "Always Allow": "Toujours autoriser",
    "Auto-connect VPN": "Connexion automatique VPN",
    "Auto-connect VPN for Wi-Fi networks": "Connexion VPN automatique sur les réseaux WiFi",
    "Automatically secure untrusted Wi-Fi networks": "Sécurise automatiquement les réseaux Wi-Fi non fiables",
    "Avira Phantom VPN Pro released!": "Avira Phantom VPN Pro est arrivé!",
    "Back": "Retour",
    "Block all internet traffic if VPN connection drops": "Bloquer tout le trafic Internet lorsque la connexion VPN est interrompue",
    "Block intrusive ads": "Bloquer la publicité intrusive",
    "Block malicious sites and content": "Bloquer les sites et contenus malveillants",
    "Buy": "Acheter",
    "Buy unlimited traffic": "Acheter du trafic illimité",
    "By proceeding, you are accepting the <a href=\"#\" ng-click=\"openEula()\">End User License Agreement (EULA)</a>, and the <a href=\"#\" ng-click=\"openTermsAndConditions()\">Terms and Conditions</a>. Avira is fulfilling its duties to provide information in accordance with Articles 13 and 14 of the General Data Protection Regulation (GDPR) with the contents of the Privacy Policy and access thereto. You can find our Privacy Policy here: <a href=\"#\" ng-click=\"openPrivacyAndPolicy()\">{{getPrivacyPolicyLink()}}</a>": "En poursuivant, vous confirmez votre acceptation du <a href=\"#\" ng-click=\"openEula()\">Contrat de licence de l'utilisateur final (CLUF)</a>, ainsi que des <a href=\"#\" ng-click=\"openTermsAndConditions()\">Conditions générales</a>. Avira remplit ses obligations d’information conformément aux articles 13 et 14 du Règlement général sur la protection des données (RGPD) de par le contenu de la Politique de confidentialité et l’accès à cette dernière. Vous pouvez consulter notre Politique de confidentialité à l'adresse : <a href=\"#\" ng-click=\"openPrivacyAndPolicy()\">{{getPrivacyPolicyLink()}}</a>",
    "Cancel": "Annuler",
    "Cannot resolve host address.": "Impossible de résoudre l'adresse hôte",
    "Change later in Settings": "Modifier plus tard dans les paramètres",
    "Check your inbox to confirm it's you. Make sure to look into your spam and junk folders as well.": "Consultez votre boîte de réception pour confirmer votre identité. Pensez à vérifier également dans les dossiers de spam et de courrier indésirable.",
    "Choose your color theme": "Choisissez votre thème de couleurs",
    "Code": "Code",
    "Collect diagnostic data": "Envoyer",
    "Connect": "Connecter",
    "Connecting": "Connexion",
    "Connecting...": "Connexion en cours…",
    "Connection to server lost.": "Connexion au serveur perdue",
    "Continue": "Continuer",
    "Dark": "Sombre",
    "Dark theme": "Thème sombre",
    "Disconnect": "Déconnecter",
    "Disconnecting": "Déconnexion",
    "Disconnecting...": "Déconnexion...",
    "Display settings": "Paramètres d’affichage",
    "Don't have an account yet?": "Pas de compte ?",
    "Don't send": "Ne pas envoyer",
    "Don't show again": "Ne plus afficher",
    "Don't show this again": "Ne plus afficher",
    "Don't want to wait? Upgrade now": "Why wait? Upgrade now",
    "Done": "Terminé",
    "Email address": "Adresse électronique",
    "Email confirmation": "Confirmation de l’e-mail",
    "Email sent": "E-mail envoyé",
    "Enjoy your unlimited data volume.<br/>Remaining days: {0}": "Profitez d'un volume de données illimité.<br/>Jours restants : {0}",
    "Enter a valid email address first": "Entrez une adresse électronique valide",
    "Enter a valid password": "Entrez un mode de passe valide",
    "Enter a valid verification code.": "Entrez un code de vérification valide.",
    "Enter your Mac password and choose <span class=\"privacyDescription__text__bold\">Always allow</span> in the next window to continue.": "Saisissez votre mot de passe Mac et dans la fenêtre suivante, sélectionnez <span class=\"privacyDescription__text__bold\">Toujours autoriser</span> pour continuer.",
    "Establishing connection...": "Établissement de la connexion en cours...",
    "Exit": "Quitter",
    "Failed to connect to the service.": "Échec de connexion au service",
    "Failed to connect using UDP protocol. Retrying with TCP protocol.": "Échec de la connexion via le protocole UDP. Nouvelle tentative avec le protocole TCP.",
    "Failed to establish the VPN connection. Please try again.": "Connexion impossible. Merci d’essayer à nouveau.",
    "Failed to purchase. Please try again later.": "L'achat a échoué. Veuillez réessayez plus tard.",
    "Fatal error.": "Erreur fatale.",
    "Fill in this field.": "Remplissez ce champ.",
    "For security reasons your account has been blocked. Please access Avira Connect to unblock it.": "Votre compte a été verrouillé. Pour le déverrouiller, accédez à Avira Connect.",
    "Forgot your password?": "Mot de passe oublié ?",
    "Get 3 Months Free": "Passer à la version Pro pour 3 mois",
    "Get Pro": "Passer à Pro",
    "Get all the protection you need.": "Obtenez toute la protection dont vous avez besoin.",
    "Get for this occasion your three-month free trial. Thank you very much for helping us in beta stage by using and testing our product.": "Offre spéciale : trois mois gratuits. Merci de nous aider à améliorer notre produit.",
    "Get started": "Démarrer",
    "Get support": "Support",
    "Get unlimited data volume now.": "Profitez d'un accès illimité.",
    "Help Avira improve its products and services by automatically sending daily anonymous diagnostic and usage data.": "Aidez Avira à améliorer ses produits et services par l’envoi automatique et anonyme de données d’activités et de diagnostic journalières.",
    "Help us improve": "Aidez-nous à nous améliorer",
    "I entered a wrong email": "L’e-mail que j’ai saisi est erroné",
    "IPSec traffic is blocked. Please contact your network administrator.": "Trafic IPSec bloqué. Contactez votre administrateur réseau.",
    "In case of technical issues you can collect diagnostic data and send a report to the developers.": "Having technical issues? We're here for you. Send us a report and we'll do the rest.",
    "Invalid credentials": "Identifiants non valide",
    "Launch at system start": "Lancer au démarrage système",
    "Length of subscription: 1 month/1 year<br><br> \n                    Price of subscription: 4,95€ per month (Mac only) – 7,95€ per month (all devices) – 59,95€ per year (all devices).<br><br>\n                    \n                    Payment will be charged to iTunes Account at confirmation of purchase. Subscription automatically renews unless auto-renew is turned off at least 24-hours before the end of the current period. Account will be charged for renewal within 24-hours prior to the end of the current period, and identify the cost of the renewal. Subscriptions may be managed by the user and auto-renewal may be turned off by going to the user's Account Settings after purchase. Any unused portion of a free trial period, if offered, will be forfeited when the user purchases a subscription to that publication, where applicable.": "Durée de l'abonnement : 1 mois/1 an - Prix de l'abonnement : 4,95 € par mois (Mac uniquement) - 7,95 € par mois (tous les appareils) - 59,95 € par an (tous les appareils). Le paiement sera facturé sur le compte iTunes après confirmation de l'achat. L'abonnement est automatiquement renouvelé sauf en cas de désactivation du renouvellement automatique au moins 24 heures avant la fin de la période en cours. Dans le cadre du renouvellement, l'abonnement sera facturé sur le compte dans les 24 heures précédant la fin de la période en cours et identifié comme tel. Les abonnements peuvent être gérés par l'utilisateur et le renouvellement automatique peut être désactivé dans les paramètres du compte après l'achat. Toute portion non utilisée de la période d'essai gratuit est perdue suite à l'achat d'un abonnement applicable au produit utilisé par l'utilisateur.",
    "Light": "Clair",
    "Light theme": "Thème clair",
    "Log in": "Connexion",
    "Log out": "Déconnexion",
    "Logging in...": "Connexion en cours…",
    "Monthly:": "Par mois :",
    "My Account": "Mon compte",
    "New to Avira?": "Nouveau chez Avira ?",
    "No Wi-Fi network saved yet for automatic VPN connection. Once saved, they will automatically appear here and can be configured.": "Aucun réseau WiFi enregistré pour la connexion automatique du VPN. Une fois enregistrés, les réseaux WiFi sont répertoriés et configurés ici.",
    "No network available.": "Aucun réseau disponible",
    "No purchase found. Nothing to restore.": "Achat introuvable. Rien à restaurer.",
    "Not Now": "Pas maintenant",
    "Not now": "Pas maintenant",
    "Okay": "OK",
    "On all my devices": "Sur tous mes appareils",
    "On my Mac": "Sur mon Mac",
    "On my PC": "Sur mon PC",
    "Oops. Sorry, there was a error in the authentication process. Try again later or contact Support.": "Oups. Désolé, une erreur s'est produite au cours du processus d'authentification. Réessayez plus tard ou contactez le support.",
    "Oops. Sorry, there was a error in the registration process. Try again later or contact Support.": "Oups. Désolé, une erreur s'est produite au cours du processus d'inscription. Réessayez plus tard ou contactez le support.",
    "Oops. Sorry, this email address is already registered with another account.": "Oups. Désolé, cette adresse électronique est déjà enregistrée dans un autre compte.",
    "Password": "Mot de passe",
    "Please register if you want to purchase Phantom VPN Pro on all your devices": "Inscrivez-vous pour acheter Phantom VPN Pro pour tous vos appareils.",
    "Privacy is important to us": "La confidentialité est essentielle à nos yeux",
    "Privacy policy": "Politique de confidentialité",
    "Purchasing...": "Achat en cours...",
    "Quit": "Quitter",
    "Rate 5 stars": "Accorder 5 étoiles",
    "Rate Avira Phantom VPN": "Évaluer Avira Phantom VPN",
    "Register": "Inscription",
    "Register for an Avira account<br>and get your 30 day free trial": "Créez un compte Avira<br>et bénéficiez de 30 jours d'essai gratuit",
    "Registering...": "Enregistrement...",
    "Renew now": "Renouveler maintenant",
    "Resend email": "Renvoyer l’e-mail",
    "Restore purchase": "Rétablir l'achat",
    "Restoring...": "Restauration en cours...",
    "Secure my connection": "Sécuriser ma connexion",
    "Securing your connection": "Sécuriser votre connexion",
    "Select virtual location": "Sélectionnez un emplacement virtuel",
    "Send": "Envoyer",
    "Send diagnostic data": "Envoyer les données du diagnostic",
    "Send feedback": "Envoyer un commentaire",
    "Send report": "Create report",
    "Settings": "Paramètres",
    "Special Offer": "Offre spéciale",
    "Subscription terms": "Conditions d'abonnement",
    "Tap Adapter not present or disabled.": "Adaptateur TAP absent ou désactivé",
    "Tell us what you think. Your feedback helps us improve.": "Envoyez-nous vos commentaires. Contribuez à l'amélioration de nos produits.",
    "Terms and conditions": "Conditions générales",
    "Test Pro for free": "Tester Pro gratuitement",
    "The amount of data that you consume. We do this to help calculate the costs of providing our VPN infrastructure": "La quantité de données que vous consommez. Cela nous permet calculer le coût de gestion de notre infrastructure VPN.",
    "The app's color theme will use the operating system setting unless you choose a different theme.": "La couleur du thème de l’application utilisera les paramètres du système d'exploitation, sauf si vous choisissez un autre thème.",
    "The app's color theme will use the operating system setting.": "La couleur du thème de l’application utilisera les paramètres du système d'exploitation.",
    "The connection is not private": "La connexion n'est pas privée",
    "The connection is private": "La connexion est privée",
    "The selected location is invalid. Please check the network connection.": "L'emplacement sélectionné n'est pas valide. Veuillez vérifier la connexion réseau.",
    "Three-Month free trial, then only <span>{0}</span> monthly": "Trois mois d’essai gratuit, puis <span>{0}</span> par mois seulement",
    "To encrypt your internet connection, Phantom VPN uses the neagent system service. It needs your permission to access the Keychain.": "Phantom VPN utilise le système de service neagent pour le chiffrement de votre connexion Internet. Sa configuration requiert votre autorisation.",
    "To try the best experience we will give you unlimited data volume for 30 days for free.": "Pour une mise en condition réelle, bénéficiez gratuitement d'un volume de données illimité pendant 30 jours.",
    "Traffic limit reached": "Limite de trafic atteinte",
    "Trial": "Version d'essai",
    "Unknown": "Inconnu",
    "Unknown error.": "Erreur inconnue.",
    "Unlimited": "Illimité",
    "Unlimited traffic": "Trafic illimité",
    "Unlimited traffic on: Windows, macOS, iOS, Android, Google Chrome": "Trafic illimité sur : Windows, macOS, iOS, Android, Google Chrome",
    "Use fastest protocol when available": "Utiliser le protocole le plus rapide disponible",
    "Use system settings": "Utiliser les paramètres du système",
    "Verification code": "Code de vérification",
    "Virtual location set to <a> {0} </a>": "Emplacement virtuel: <a> {0} </a>",
    "Virtual location: {0}": "Emplacement virtuel : {0}",
    "We sent a verification code to your phone number": "Nous avons envoyé un code de vérification à votre numéro de téléphone.",
    "We want to be fully transparent about the data you agree to share with us. To provide you a smooth VPN experience we need a minimum set of data:": "Nous souhaitons une transparence totale sur les données que vous acceptez de partager avec nous. Pour vous offrir une expérience VPN des plus satisfaisantes, nous avons besoin d’un minimum de données :",
    "Whether you are a free or a paid user. It's important for our communications to be able to differentiate the two": "La version que vous utilisez, gratuite ou payante. Il est important que nous puissions faire la distinction entre ces deux offres.",
    "Yearly:": "Par an :",
    "You will be disconnected in {0} seconds.": "Vous serez déconnecté dans {0} secondes.",
    "Your connection is secure": "Connexion sécurisée",
    "Your connection is secure.": "Connexion sécurisée",
    "Your connection is unsecure": "Connexion non sécurisée",
    "Your internet connection was protected from prying eyes. Is this worth 5 stars?": "Votre connexion Internet a été protégée contre les regards curieux. Ceci mérite-t-il 5 étoiles ?",
    "Your license will expire soon.<br/>Remaining days: {0}": "Votre licence expire bientôt.<br/>Jours restants : {0}",
    "Your password must contain at least 8 characters, one digit, and one uppercase letter.": "Votre mot de passe doit contenir au moins 8 caractères, un chiffre et une lettre majuscule.",
    "default": "par défaut",
    "https://www.research.net/r/HLV5Q2H?": "https://www.research.net/r/HLV5Q2H?",
    "https://www.research.net/r/J5H53HC?": "https://www.research.net/r/J5H53HC?",
    "or get 500 MB if you <a> register </a>": "ou récupérez 500 Mo en vous <a> enregistrant </a>",
    "{0} out of {1} daily secured traffic": "{0} sur {1} de trafic quotidien sécurisé",
    "{0} out of {1} monthly secured traffic": "{0} sur {1} de trafic mensuel sécurisé",
    "{0} out of {1} weekly secured traffic": "{0} sur {1} de trafic hebdomadaire sécurisé",
    "{0} secured traffic": "{0} de trafic sécurisé",
    "%": "%",
    "{0} used": "{0} utilisé(s)"
  });
  /* jshint +W100 */
}]);

},{}],63:[function(require,module,exports){
"use strict";

angular.module('LauncherApp').run(['gettextCatalog', function (gettextCatalog) {
  /* jshint -W100 */
  gettextCatalog.setStrings('it_IT', {
    "0 bytes": "0 byte",
    "<span class=\"privacyDescription__text__bold\">We don't log your online activity or any information that can link you to any action</span>, such as downloading a file, or visiting a particular website.": "<span class=\"privacyDescription__text__bold\">Non registriamo le tue attività online o le informazioni che possono collegare a te qualsiasi azione</span>, come il download di un file o la visita a uno specifico sito web.",
    "A new driver has been installed, please restart your computer to complete the installation": "Riavviare il computer prima di proteggere la connessione.",
    "About": "Informazioni",
    "Access": "Continua",
    "Account details": "Dettagli dell'account",
    "Agree and continue": "Acconsenti e continua",
    "All free slots are currently taken.": "No slots free currently.",
    "Already have an account?": "Hai già un account?",
    "Always Allow": "Consenti sempre",
    "Auto-connect VPN": "Connessione automatica VPN",
    "Auto-connect VPN for Wi-Fi networks": "Connessione automatica VPN per reti Wi-Fi",
    "Automatically secure untrusted Wi-Fi networks": "Proteggi automaticamente le reti Wi-Fi non sicure",
    "Avira Phantom VPN Pro released!": "Avira Phantom VPN Pro è arrivata!",
    "Back": "Indietro",
    "Block all internet traffic if VPN connection drops": "Blocca tutto il traffico Internet se la connessione VPN si interrompe",
    "Block intrusive ads": "Blocca pubblicità invadente",
    "Block malicious sites and content": "Blocca siti e contenuti dannosi",
    "Buy": "Acquista",
    "Buy unlimited traffic": "Acquista traffico illimitato",
    "By proceeding, you are accepting the <a href=\"#\" ng-click=\"openEula()\">End User License Agreement (EULA)</a>, and the <a href=\"#\" ng-click=\"openTermsAndConditions()\">Terms and Conditions</a>. Avira is fulfilling its duties to provide information in accordance with Articles 13 and 14 of the General Data Protection Regulation (GDPR) with the contents of the Privacy Policy and access thereto. You can find our Privacy Policy here: <a href=\"#\" ng-click=\"openPrivacyAndPolicy()\">{{getPrivacyPolicyLink()}}</a>": "Continuando confermi di accettare il <a href=\"#\" ng-click=\"openEula()\">Contratto di licenza con l'utente finale (EULA)</a> e i <a href=\"#\" ng-click=\"openTermsAndConditions()\">Termini e condizioni</a>. Avira adempie al proprio obbligo di fornire informazioni ai sensi degli articoli 13 e 14 del Regolamento generale sulla protezione dei dati (RGPD) mediante i contenuti dell'Informativa sulla privacy e l'accesso agli stessi. La nostra Informativa sulla privacy è disponibile qui: <a href=\"#\" ng-click=\"openPrivacyAndPolicy()\">{{getPrivacyPolicyLink()}}</a>",
    "Cancel": "Annulla",
    "Cannot resolve host address.": "Indirizzo host sconosciuto",
    "Change later in Settings": "Cambia in seguito in Impostazioni",
    "Check your inbox to confirm it's you. Make sure to look into your spam and junk folders as well.": "Controlla la posta in arrivo per confermare la tua identità. Controlla anche le cartelle dello spam e della posta indesiderata.",
    "Choose your color theme": "Scegli il colore del display",
    "Code": "Codice",
    "Collect diagnostic data": "Invia",
    "Connect": "Connettiti",
    "Connecting": "Connessione in corso",
    "Connecting...": "Connessione in corso...",
    "Connection to server lost.": "Connessione con il server persa",
    "Continue": "Continua",
    "Dark": "Scuro",
    "Dark theme": "Scuro",
    "Disconnect": "Disconnetti",
    "Disconnecting": "Disconnessione in corso",
    "Disconnecting...": "Disconnessione in corso...",
    "Display settings": "Impostazioni display",
    "Don't have an account yet?": "Nessun account?",
    "Don't send": "Non inviare",
    "Don't show again": "Non mostrare più",
    "Don't show this again": "Non mostrare più",
    "Don't want to wait? Upgrade now": "Why wait? Upgrade now",
    "Done": "Fine",
    "Email address": "Indirizzo email",
    "Email confirmation": "Conferma email",
    "Email sent": "Email inviata",
    "Enjoy your unlimited data volume.<br/>Remaining days: {0}": "Approfitta del volume dati illimitato.<br/>Giorni rimanenti: {0}",
    "Enter a valid email address first": "Inserisci un indirizzo email valido",
    "Enter a valid password": "Inserisci una password valida",
    "Enter a valid verification code.": "Inserisci un codice di verifica valido.",
    "Enter your Mac password and choose <span class=\"privacyDescription__text__bold\">Always allow</span> in the next window to continue.": "Inserisci la password del Mac e nella schermata successiva seleziona <span class=\"privacyDescription__text__bold\">Consenti sempre</span> per continuare.",
    "Establishing connection...": "Tentativo di connessione in corso...",
    "Exit": "Esci",
    "Failed to connect to the service.": "Impossibile connettersi al servizio",
    "Failed to connect using UDP protocol. Retrying with TCP protocol.": "Connessione non riuscita con il protocollo UDP. Riprova con il protocollo TCP.",
    "Failed to establish the VPN connection. Please try again.": "Connessione impossibile. Riprova.",
    "Failed to purchase. Please try again later.": "Acquisto non riuscito. Riprova più tardi.",
    "Fatal error.": "Errore irreversibile.",
    "Fill in this field.": "Compila questo campo.",
    "For security reasons your account has been blocked. Please access Avira Connect to unblock it.": "Il tuo account è stato bloccato. Puoi sbloccarlo semplicemente accedendo ad Avira Connect.",
    "Forgot your password?": "Hai dimenticato la password?",
    "Get 3 Months Free": "Passa a Pro per 3 mesi",
    "Get Pro": "Passa a Pro",
    "Get all the protection you need.": "Ottieni tutta la protezione di cui hai bisogno.",
    "Get for this occasion your three-month free trial. Thank you very much for helping us in beta stage by using and testing our product.": "Ricevi l'offerta gratuita di 3 mesi. Hai deciso di provare il nostro prodotto e aiutarci a renderlo migliore: grazie!",
    "Get started": "Inizia ora",
    "Get support": "Richiedi supporto",
    "Get unlimited data volume now.": "Ottieni accesso illimitato ora.",
    "Help Avira improve its products and services by automatically sending daily anonymous diagnostic and usage data.": "Aiuta Avira a migliorare prodotti e servizi inviando automaticamente ogni giorno dati diagnostici e d'uso anonimi.",
    "Help us improve": "Aiutaci a migliorare",
    "I entered a wrong email": "Ho inserito un indirizzo email sbagliato",
    "IPSec traffic is blocked. Please contact your network administrator.": "Il traffico IPSec è bloccato. Contatta l'amministratore di rete.",
    "In case of technical issues you can collect diagnostic data and send a report to the developers.": "Having technical issues? We're here for you. Send us a report and we'll do the rest.",
    "Invalid credentials": "Credenziali non valide",
    "Launch at system start": "Lancia all'avvio del sistema",
    "Length of subscription: 1 month/1 year<br><br> \n                    Price of subscription: 4,95€ per month (Mac only) – 7,95€ per month (all devices) – 59,95€ per year (all devices).<br><br>\n                    \n                    Payment will be charged to iTunes Account at confirmation of purchase. Subscription automatically renews unless auto-renew is turned off at least 24-hours before the end of the current period. Account will be charged for renewal within 24-hours prior to the end of the current period, and identify the cost of the renewal. Subscriptions may be managed by the user and auto-renewal may be turned off by going to the user's Account Settings after purchase. Any unused portion of a free trial period, if offered, will be forfeited when the user purchases a subscription to that publication, where applicable.": "Durata dell'abbonamento: 1 mese/1 anno - Prezzo dell'abbonamento: 4,95 € al mese (solo Mac) / 7,95 € al mese (tutti i dispositivi) / 59,95 € all'anno (tutti i dispositivi). L'importo viene addebitato sull'account iTunes alla conferma dell'acquisto. L'abbonamento si rinnova automaticamente salvo nel caso in cui l'opzione di rinnovo automatico non venga disattivata almeno 24 ore prima della fine del periodo attuale. Il costo del rinnovo viene addebitato sull'account nelle 24 ore precedenti la fine del periodo attuale. Gli abbonamenti possono essere gestiti dall'utente e il rinnovo automatico può essere disattivato accedendo alle Impostazioni dell'account utente dopo l'acquisto. Ove previsto, il diritto a godere di una qualsiasi porzione inutilizzata di un periodo di prova gratuito, se offerto, viene meno nel momento in cui l'utente acquista un abbonamento per la pubblicazione in questione.",
    "Light theme": "Chiaro",
    "Log in": "Accedi",
    "Log out": "Esci",
    "Logging in...": "Accesso in corso...",
    "Monthly:": "Mensile:",
    "My Account": "Il mio account",
    "New to Avira?": "Non conosci Avira?",
    "No Wi-Fi network saved yet for automatic VPN connection. Once saved, they will automatically appear here and can be configured.": "Nessuna rete Wi-Fi memorizzata per connessione automatica VPN. Le reti memorizzate appaiono automaticamente qui e possono essere configurate.",
    "No network available.": "Nessuna rete disponibile",
    "No purchase found. Nothing to restore.": "Nessun acquisto trovato Nulla da ripristinare.",
    "Not Now": "Non ora",
    "Not now": "Non ora",
    "Okay": "OK",
    "On all my devices": "Su tutti i miei dispositivi",
    "On my Mac": "Sul mio Mac",
    "On my PC": "Sul mio PC",
    "Oops. Sorry, there was a error in the authentication process. Try again later or contact Support.": "Oops. Purtroppo si è verificato un errore durante l'autenticazione. Riprova più tardi o contatta il Supporto.",
    "Oops. Sorry, there was a error in the registration process. Try again later or contact Support.": "Oops. Purtroppo si è verificato un errore durante la registrazione. Riprova più tardi o contatta il Supporto.",
    "Oops. Sorry, this email address is already registered with another account.": "Oops. Purtroppo questo indirizzo email è già registrato in un altro account.",
    "Password": "Password",
    "Please register if you want to purchase Phantom VPN Pro on all your devices": "Registrati per acquistare Phantom VPN Pro per tutti i tuoi dispositivi.",
    "Privacy is important to us": "La privacy è importante per noi",
    "Privacy policy": "Policy sulla privacy",
    "Purchasing...": "Acquisto in corso...",
    "Quit": "Esci",
    "Rate 5 stars": "Assegna 5 stelle",
    "Rate Avira Phantom VPN": "Dai un voto a Avira Phantom VPN",
    "Register": "Registrati",
    "Register for an Avira account<br>and get your 30 day free trial": "Registra un account Avira<br>e ricevi la prova gratuita di 30 giorni",
    "Registering...": "Registrazione...",
    "Renew now": "Rinnova ora",
    "Resend email": "Rinvia email",
    "Restore purchase": "Ripristina acquisto",
    "Restoring...": "Ripristino...",
    "Secure my connection": "Proteggi la connessione",
    "Securing your connection": "Proteggi la tua connessione",
    "Select virtual location": "Seleziona la posizione virtuale",
    "Send": "Invia",
    "Send diagnostic data": "Invia dati di diagnostica",
    "Send feedback": "Invia feedback",
    "Send report": "Create report",
    "Settings": "Impostazioni",
    "Special Offer": "Offerta speciale",
    "Subscription terms": "Termini dell'abbonamento",
    "Tap Adapter not present or disabled.": "Adattatore TAP assente o disabilitato",
    "Tell us what you think. Your feedback helps us improve.": "Dicci cosa pensi. Il tuo feedback ci aiuta a migliorare.",
    "Terms and conditions": "Termini e condizioni",
    "Test Pro for free": "Prova Pro gratis",
    "The amount of data that you consume. We do this to help calculate the costs of providing our VPN infrastructure": "La quantità di dati che consumi. È necessario per calcolare quanto costa fornire la nostra infrastruttura VPN.",
    "The app's color theme will use the operating system setting unless you choose a different theme.": "L'app adotterà il colore impostato nel sistema operativo, a meno che tu non ne scelga uno diverso.",
    "The app's color theme will use the operating system setting.": "L'app adotterà il colore impostato nel sistema operativo.",
    "The connection is not private": "La connessione non è privata",
    "The connection is private": "La connessione è privata",
    "The selected location is invalid. Please check the network connection.": "La posizione selezionata non è valida. Controlla la connessione di rete.",
    "Three-Month free trial, then only <span>{0}</span> monthly": "Versione di prova gratuita per tre mesi, poi solo <span>{0}</span> al mese",
    "To encrypt your internet connection, Phantom VPN uses the neagent system service. It needs your permission to access the Keychain.": "Phantom VPN utilizza il servizio di sistema neagent per crittografare la connessione a Internet. Ci serve la tua autorizzazione per installarlo.",
    "To try the best experience we will give you unlimited data volume for 30 days for free.": "Per farti provare l'esperienza migliore ti offriamo gratis un volume dati illimitato per 30 giorni.",
    "Traffic limit reached": "Limite di traffico raggiunto",
    "Trial": "Versione di prova",
    "Unknown": "Sconosciuto",
    "Unknown error.": "Errore sconosciuto.",
    "Unlimited": "Illimitato",
    "Unlimited traffic": "Traffico illimitato",
    "Unlimited traffic on: Windows, macOS, iOS, Android, Google Chrome": "Traffico illimitato su: Windows, macOS, iOS, Android, Google Chrome",
    "Use fastest protocol when available": "Usa il protocollo più rapido se disponibile",
    "Use system settings": "Usa impostazioni di sistema",
    "Verification code": "Codice di verifica",
    "Virtual location set to <a> {0} </a>": "Posizione virtuale: <a> {0} </a>",
    "Virtual location: {0}": "Posizione virtuale: {0}",
    "We sent a verification code to your phone number": "Abbiamo inviato un codice di verifica al tuo numero di telefono.",
    "We want to be fully transparent about the data you agree to share with us. To provide you a smooth VPN experience we need a minimum set of data:": "Desideriamo essere del tutto trasparenti sui dati che acconsenti di condividere con noi. Per fornirti un'esperienza confortevole con la VPN ci serve sapere solo:",
    "Whether you are a free or a paid user. It's important for our communications to be able to differentiate the two": "Se sei un utente pagante o gratuito. Per le nostre comunicazioni, è importante differenziare le due tipologie di utente.",
    "Yearly:": "Annuale:",
    "You will be disconnected in {0} seconds.": "La disconnessione avverrà entro {0} secondi.",
    "Your connection is secure": "La connessione è sicura",
    "Your connection is secure.": "La connessione è sicura",
    "Your connection is unsecure": "La connessione non è sicura",
    "Your internet connection was protected from prying eyes. Is this worth 5 stars?": "La tua connessione a Internet è stata protetta da sguardi indiscreti. Merita 5 stelle?",
    "Your license will expire soon.<br/>Remaining days: {0}": "La tua licenza scadrà a breve.<br/> Giorni rimanenti: {0}",
    "Your password must contain at least 8 characters, one digit, and one uppercase letter.": "La password deve contenere almeno 8 caratteri, una cifra e una lettera maiuscola.",
    "default": "predefinito",
    "https://www.research.net/r/HLV5Q2H?": "https://www.research.net/r/HLV5Q2H?",
    "https://www.research.net/r/J5H53HC?": "https://www.research.net/r/J5H53HC?",
    "or get 500 MB if you <a> register </a>": "oppure ottieni 500 MB <a> registrandoti </a>",
    "{0} out of {1} daily secured traffic": "{0} di {1} di traffico giornaliero protetto",
    "{0} out of {1} monthly secured traffic": "{0} di {1} di traffico mensile protetto",
    "{0} out of {1} weekly secured traffic": "{0} di {1} di traffico settimanale protetto",
    "{0} secured traffic": "{0} di traffico sicuro",
    "%": "%",
    "{0} used": "{0} utilizzato"
  });
  /* jshint +W100 */
}]);

},{}],64:[function(require,module,exports){
"use strict";

angular.module('LauncherApp').run(['gettextCatalog', function (gettextCatalog) {
  /* jshint -W100 */
  gettextCatalog.setStrings('ja_JP', {
    "0 bytes": "0 バイト",
    "<span class=\"privacyDescription__text__bold\">We don't log your online activity or any information that can link you to any action</span>, such as downloading a file, or visiting a particular website.": "<span class=\"privacyDescription__text__bold\">弊社は、ファイルのダウンロードや特定のウェブサイトへの訪問など、あなたのオンライン アクティビティや、</span>あなたがなんらかのアクションと関連付けられる情報を記録しません。",
    "A new driver has been installed, please restart your computer to complete the installation": "接続を保護する前に、コンピュータを再起動してください。",
    "About": "情報",
    "Access": "継続",
    "Account details": "アカウント詳細",
    "Agree and continue": "同意して続行",
    "All free slots are currently taken.": "No slots free currently.",
    "Already have an account?": "すでにアカウントをお持ちですか？?",
    "Always Allow": "常に許可",
    "Auto-connect VPN": "VPN に自動接続する",
    "Auto-connect VPN for Wi-Fi networks": "Wi-Fi ネットワークに対して VPN に自動接続する",
    "Automatically secure untrusted Wi-Fi networks": "自動的に信頼されていない Wi-Fi ネットワークを保護します",
    "Avira Phantom VPN Pro released!": "Avira Phantom VPN Pro が入手可能になりました！!",
    "Back": "戻る",
    "Block all internet traffic if VPN connection drops": "VPN 接続が切断された場合には、すべてのインターネット トラフィックをブロックします",
    "Block intrusive ads": "侵入型広告をブロックする",
    "Block malicious sites and content": "悪意のあるサイトとコンテンツをブロック",
    "Buy": "購入",
    "Buy unlimited traffic": "無制限のトラフィックを購入",
    "By proceeding, you are accepting the <a href=\"#\" ng-click=\"openEula()\">End User License Agreement (EULA)</a>, and the <a href=\"#\" ng-click=\"openTermsAndConditions()\">Terms and Conditions</a>. Avira is fulfilling its duties to provide information in accordance with Articles 13 and 14 of the General Data Protection Regulation (GDPR) with the contents of the Privacy Policy and access thereto. You can find our Privacy Policy here: <a href=\"#\" ng-click=\"openPrivacyAndPolicy()\">{{getPrivacyPolicyLink()}}</a>": "続行することで、<a href=\"#\" ng-click=\"openEula()\">エンドユーザー使用許諾契約書 (EULA)</a> および<a href=\"#\" ng-click=\"openTermsAndConditions()\">ビジネス標準契約条件</a>に同意したものとみなされます。Aviraはプライバシー ポリシーの内容とそれへのアクセスを以って、EU一般データ保護規則 (GDPR) 第13条および第14条に準拠した情報提供義務を果たしています。プライバシー ポリシーはこちらをご覧ください: <a href=\"#\" ng-click=\"openPrivacyAndPolicy()\">{{getPrivacyPolicyLink()}}</a>",
    "Cancel": "キャンセル",
    "Cannot resolve host address.": "ホスト アドレスを解決できません。",
    "Change later in Settings": "後で設定で変更する",
    "Check your inbox to confirm it's you. Make sure to look into your spam and junk folders as well.": "メール ボックスをチェックして身分を確認してください。迷惑メールとジャンク メールのフォルダーを確認してください.",
    "Choose your color theme": "カラーテーマをお選びください",
    "Code": "コード",
    "Collect diagnostic data": "送信",
    "Connect": "接続",
    "Connecting": "接続中",
    "Connecting...": "接続中…",
    "Connection to server lost.": "サーバーへの接続が失われました。",
    "Continue": "継続",
    "Dark": "ダーク",
    "Dark theme": "ダーク テーマ",
    "Disconnect": "接続の切断",
    "Disconnecting": "接続を切断中",
    "Disconnecting...": "接続を切断中...",
    "Display settings": "ディスプレイ設定",
    "Don't have an account yet?": "アカウントがありませんか？",
    "Don't send": "送信しない",
    "Don't show again": "再度表示しない",
    "Don't show this again": "今後表示しない",
    "Don't want to wait? Upgrade now": "Why wait? Upgrade now",
    "Done": "完了",
    "Email address": "メール アドレス",
    "Email confirmation": "電子メールによる確認",
    "Email sent": "電子メール送信済み",
    "Enjoy your unlimited data volume.<br/>Remaining days: {0}": "無制限のデータ量をお楽しみください。<br/>残り日数：{0}",
    "Enter a valid email address first": "有効なメール アドレスを入力してください",
    "Enter a valid password": "有効なパスワードを入力してください",
    "Enter a valid verification code.": "有効な検証コードを入力してください.",
    "Enter your Mac password and choose <span class=\"privacyDescription__text__bold\">Always allow</span> in the next window to continue.": "続行するにはMacパスワードを入力し、次のウィンドウで<span class=\"privacyDescription__text__bold\">常に許可</span>を選択します。",
    "Establishing connection...": "接続を確立中...",
    "Exit": "終了",
    "Failed to connect to the service.": "サービスへの接続に失敗しました。",
    "Failed to connect using UDP protocol. Retrying with TCP protocol.": "UDP プロトコルを使用した接続に失敗しました。TCP プロトコルを使用して再試行します.",
    "Failed to establish the VPN connection. Please try again.": "接続できません。再試行してください。",
    "Failed to purchase. Please try again later.": "購入に失敗しました。後で再試行してください。",
    "Fatal error.": "致命的なエラー.",
    "Fill in this field.": "このフィールドに入力してください.",
    "For security reasons your account has been blocked. Please access Avira Connect to unblock it.": "アカウントがロックされています。Avira Connect にアクセスすることでロック解除できます。",
    "Forgot your password?": "Parolanızı mı unuttunuz?",
    "Get 3 Months Free": "3 か月間の Pro を購入",
    "Get Pro": "Pro を購入",
    "Get all the protection you need.": "必要なすべての保護を取得。",
    "Get for this occasion your three-month free trial. Thank you very much for helping us in beta stage by using and testing our product.": "3 か月間無料スペシャル オファーを入手。弊社の製品を試用し、改善にご協力いただきありがとうございます。",
    "Get started": "開始する",
    "Get support": "サポートを得る",
    "Get unlimited data volume now.": "無制限アクセスを今すぐ入手。",
    "Help Avira improve its products and services by automatically sending daily anonymous diagnostic and usage data.": "毎日の匿名診断および使用データを自動的に送信して、Avira の製品およびサービス改善にご協力ください。",
    "Help us improve": "改善にご協力ください",
    "I entered a wrong email": "入力した電子メールが正しくありません",
    "IPSec traffic is blocked. Please contact your network administrator.": "IPSec トラフィックはブロックされています。ネットワーク管理者に連絡してください。",
    "In case of technical issues you can collect diagnostic data and send a report to the developers.": "Having technical issues? We're here for you. Send us a report and we'll do the rest.",
    "Invalid credentials": "無効な認証情報",
    "Launch at system start": "システム起動時に起動",
    "Length of subscription: 1 month/1 year<br><br> \n                    Price of subscription: 4,95€ per month (Mac only) – 7,95€ per month (all devices) – 59,95€ per year (all devices).<br><br>\n                    \n                    Payment will be charged to iTunes Account at confirmation of purchase. Subscription automatically renews unless auto-renew is turned off at least 24-hours before the end of the current period. Account will be charged for renewal within 24-hours prior to the end of the current period, and identify the cost of the renewal. Subscriptions may be managed by the user and auto-renewal may be turned off by going to the user's Account Settings after purchase. Any unused portion of a free trial period, if offered, will be forfeited when the user purchases a subscription to that publication, where applicable.": "サブスクリプション期間：1 か月/1年 - サブスクリプション料金：4.95€/1 か月（Mac のみ）- 7.95€/1 か月（全デバイス）- 59.95€/1 年（全デバイス）。料金は、購入の確認の際に iTunes アカウントに請求されます。サブスクリプションは、自動更新が現在の試用期間終了の 24 時間前にオフに設定されていない限り、自動的に更新されます。現在の試用期間終了の 24 時間以内に、アカウントに対して請求が行われ、更新料金が特定されます。サブスクリプションは、ユーザーが管理できます。自動更新は、購入後にユーザーのアカウント設定に移動することでオフに切り替えることができます。無料使用期間の未使用部分が提供されている場合、ユーザーがそのパブリケーションに対するサブスクリプションを購入した際に失われます（該当する場合）.",
    "Light": "ライト",
    "Light theme": "ライト テーマ",
    "Log in": "ログイン",
    "Log out": "ログアウト",
    "Logging in...": "ログイン中…",
    "Monthly:": "毎月：",
    "My Account": "マイ アカウント",
    "New to Avira?": "Avira を使い始めて、まだ間もないですか？",
    "No Wi-Fi network saved yet for automatic VPN connection. Once saved, they will automatically appear here and can be configured.": "VPN 自動接続のために保存された Wi-Fi ネットワークはありません。保存されると、ここに自動的に表示され、構成できます.",
    "No network available.": "利用可能なネットワークがありません。",
    "No purchase found. Nothing to restore.": "購入が見つかりません。復元するものはありません。",
    "Not Now": "後で",
    "Not now": "後で",
    "Okay": "OK",
    "On all my devices": "すべてのマイ デバイス",
    "On my Mac": "Mac のみ",
    "On my PC": "自分のPC上",
    "Oops. Sorry, there was a error in the authentication process. Try again later or contact Support.": "申し訳ございません。申し訳ございません。認証プロセスにエラーが発生しました。後で再度試みるか、サポートに連絡してください。",
    "Oops. Sorry, there was a error in the registration process. Try again later or contact Support.": "申し訳ございません。申し訳ございません。登録プロセスにエラーが発生しました。後で再度試みるか、サポートに連絡してください。",
    "Oops. Sorry, this email address is already registered with another account.": "申し訳ございません。この電子メールは既に別のアカウントで登録されています.",
    "Password": "パスワード",
    "Please register if you want to purchase Phantom VPN Pro on all your devices": "登録してお使いのデバイスすべてにPhantom VPN Proを購入.",
    "Privacy is important to us": "プライバシーは弊社にとって重要です",
    "Privacy policy": "プライバシー ポリシー",
    "Purchasing...": "購入しています...",
    "Quit": "終了",
    "Rate 5 stars": "5 つ星で評価",
    "Rate Avira Phantom VPN": "Avira Phantom VPNを評価",
    "Register": "登録",
    "Register for an Avira account<br>and get your 30 day free trial": "Avira アカウントに登録すると、<br>30 日間の無料トライアル版を入手できます",
    "Registering...": "登録中...",
    "Renew now": "すぐに更新する",
    "Resend email": "電子メールを再送信",
    "Restore purchase": "購入を復元する",
    "Restoring...": "復元しています...",
    "Secure my connection": "接続を保護する",
    "Securing your connection": "接続の保護",
    "Select virtual location": "仮想位置を選択する",
    "Send": "送信",
    "Send diagnostic data": "診断データを送信",
    "Send feedback": "フィードバックの送信",
    "Send report": "Create report",
    "Settings": "設定",
    "Special Offer": "スペシャル オファー",
    "Subscription terms": "サブスクリプション期間",
    "Tap Adapter not present or disabled.": "TAP アダプターは存在しないか、無効になっています。",
    "Tell us what you think. Your feedback helps us improve.": "ご意見をお知らせください。ユーザーのフィードバッグは、弊社の向上に役立ちます。",
    "Terms and conditions": "ビジネス標準契約条件",
    "Test Pro for free": "Pro の無料お試し",
    "The amount of data that you consume. We do this to help calculate the costs of providing our VPN infrastructure": "あなたが消費するデータ量。弊社はこれを用いて VPN インフラストラクチャー提供の費用を算出します.",
    "The app's color theme will use the operating system setting unless you choose a different theme.": "別のテーマが選択された場合を除いて、オペレーティングシステム設定がアプリのカラーテーマに使用されます。",
    "The app's color theme will use the operating system setting.": "オペレーティングシステム設定がアプリのカラーテーマに使用されます。",
    "The connection is not private": "接続はプライベートではありません",
    "The connection is private": "接続はプライベートです",
    "The selected location is invalid. Please check the network connection.": "選択した位置は無効です。ネットワークの接続を確認してください.",
    "Three-Month free trial, then only <span>{0}</span> monthly": "3 ヵ月間無料トライアル版。そしてその後は、わずか月次 <span>{0}</span>",
    "To encrypt your internet connection, Phantom VPN uses the neagent system service. It needs your permission to access the Keychain.": "Phantom VPNは、インターネット接続を暗号化するためにneagentシステムサービスを使用します。セットアップにはユーザーの許可が必要です。",
    "To try the best experience we will give you unlimited data volume for 30 days for free.": "最高の試用を可能にするために、無制限のデータ量を無料で 30 日間提供します.",
    "Traffic limit reached": "トラフィックが限界に達しました",
    "Trial": "無料トライアル版",
    "Unknown": "不明",
    "Unknown error.": "不明なエラー.",
    "Unlimited": "無制限",
    "Unlimited traffic": "無制限のトラフィック",
    "Unlimited traffic on: Windows, macOS, iOS, Android, Google Chrome": "無制限のトラフィック：Windows、macOS、iOS、Android、Google Chrome",
    "Use fastest protocol when available": "可能であれば、最速のプロトコルを使用",
    "Use system settings": "システム設定を使用する",
    "Verification code": "検証コード",
    "Virtual location set to <a> {0} </a>": "仮想位置は次にあります: <a> {0} </a>",
    "Virtual location: {0}": "仮想位置は次にあります: {0}",
    "We sent a verification code to your phone number": "あなたの電話番号に検証コードを送信しました.",
    "We want to be fully transparent about the data you agree to share with us. To provide you a smooth VPN experience we need a minimum set of data:": "皆様が共有することに同意いただいたデータについて完全にトランスペアレントな状態にしたいと考えています。スムーズな VPN 体験を提供するには、次のデータ セットが最低限必要になります。",
    "Whether you are a free or a paid user. It's important for our communications to be able to differentiate the two": "あなたが無料ユーザーであるか有料ユーザーであるか。弊社の通信において、この 2 つを識別できることが重要になります。",
    "Yearly:": "毎年：",
    "You will be disconnected in {0} seconds.": "{0} 秒で接続が切断されます.",
    "Your connection is secure": "接続は安全です",
    "Your connection is secure.": "接続は安全です",
    "Your connection is unsecure": "接続は安全ではありません",
    "Your internet connection was protected from prying eyes. Is this worth 5 stars?": "お使いのインターネット接続は、詮索から保護されていました。5 つ星の評価に値するとは思いませんか？",
    "Your license will expire soon.<br/>Remaining days: {0}": "ライセンスは間もなく終了します。<br/>残り日数：{0}",
    "Your password must contain at least 8 characters, one digit, and one uppercase letter.": "パスワードには、8 文字以上で、1 つの数字と 1 つの大文字の英字を含んでいる必要があります.",
    "default": "デフォルト",
    "https://www.research.net/r/HLV5Q2H?": "https://www.research.net/r/HLV5Q2H?",
    "https://www.research.net/r/J5H53HC?": "https://www.research.net/r/J5H53HC?",
    "or get 500 MB if you <a> register </a>": "または<a> 登録 </a>すれば、500 MB が得られます",
    "{0} out of {1} daily secured traffic": "{0}/{1} 日単位のセキュアなトラフィック",
    "{0} out of {1} monthly secured traffic": "{0}/{1} 月単位のセキュアなトラフィック",
    "{0} out of {1} weekly secured traffic": "{0}/{1} 週単位のセキュアなトラフィック",
    "{0} secured traffic": "{0} のセキュアなトラフィック",
    "%": "%",
    "{0} used": "{0} 使用済み"
  });
  /* jshint +W100 */
}]);

},{}],65:[function(require,module,exports){
"use strict";

angular.module('LauncherApp').run(['gettextCatalog', function (gettextCatalog) {
  /* jshint -W100 */
  gettextCatalog.setStrings('nl_NL', {
    "0 bytes": "0 bytes",
    "<span class=\"privacyDescription__text__bold\">We don't log your online activity or any information that can link you to any action</span>, such as downloading a file, or visiting a particular website.": "<span class=\"privacyDescription__text__bold\">We registreren geen online activiteiten en andere informatie die aan u kan worden gekoppeld</span>, zoals het downloaden van een bestand of het bekijken van een specifieke website.",
    "A new driver has been installed, please restart your computer to complete the installation": "Herstart uw computer vóór het beveiligen van uw verbinding.",
    "About": "Info",
    "Access": "Doorgaan",
    "Account details": "Accountgegevens",
    "Agree and continue": "Akkoord en doorgaan",
    "All free slots are currently taken.": "No slots free currently.",
    "Already have an account?": "Hebt u al een account?",
    "Always Allow": "Altijd toestaan",
    "Auto-connect VPN": "Automatisch verbinden met VPN",
    "Auto-connect VPN for Wi-Fi networks": "Automatisch verbinden met VPN voor wifinetwerken.",
    "Automatically secure untrusted Wi-Fi networks": "Automatisch niet-vertrouwde wifi-netwerken beveiligen",
    "Avira Phantom VPN Pro released!": "Avira Phantom VPN Pro is hier!",
    "Back": "Terug",
    "Block all internet traffic if VPN connection drops": "Al het internetverkeer blokkeren als de VPN-verbinding wegvalt",
    "Block intrusive ads": "Blokkeer opdringerige advertenties",
    "Block malicious sites and content": "Schadelijke sites en inhoud blokkeren",
    "Buy": "Kopen",
    "Buy unlimited traffic": "Onbeperkt verkeer kopen",
    "By proceeding, you are accepting the <a href=\"#\" ng-click=\"openEula()\">End User License Agreement (EULA)</a>, and the <a href=\"#\" ng-click=\"openTermsAndConditions()\">Terms and Conditions</a>. Avira is fulfilling its duties to provide information in accordance with Articles 13 and 14 of the General Data Protection Regulation (GDPR) with the contents of the Privacy Policy and access thereto. You can find our Privacy Policy here: <a href=\"#\" ng-click=\"openPrivacyAndPolicy()\">{{getPrivacyPolicyLink()}}</a>": "Als u doorgaat, accepteert u de <a href=\"#\" ng-click=\"openEula()\">licentieovereenkomst voor eindgebruikers (EULA) </a> en de <a href=\"#\" ng-click=\"openTermsAndConditions()\">algemene voorwaarden</a>. Avira voert met de inhoud van het privacybeleid en de toegang daartoe de verplichtingen uit om informatie te leveren in overeenstemming met artikel 13 en 14 van de Algemene verordening gegevensbescherming (AVG). U kunt ons privacybeleid hier vinden: <a href=\"#\" ng-click=\"openPrivacyAndPolicy()\">{{getPrivacyPolicyLink()}}</a>",
    "Cancel": "Annuleren",
    "Cannot resolve host address.": "Kan hostadres niet verwerken.",
    "Change later in Settings": "Later in Instellingen wijzigen",
    "Check your inbox to confirm it's you. Make sure to look into your spam and junk folders as well.": "Bekijk uw Postvak IN om te bevestigen dat u het bent. Controleer ook uw spammap en de map ongewenste mail.",
    "Choose your color theme": "Kies uw kleurenthema",
    "Code": "Code",
    "Collect diagnostic data": "Verzenden",
    "Connect": "Verbinden",
    "Connecting": "Verbinding maken",
    "Connecting...": "Verbinden…",
    "Connection to server lost.": "Verbinding met server verbroken",
    "Continue": "Doorgaan",
    "Dark": "Donker",
    "Dark theme": "Donker thema",
    "Disconnect": "Verbinding verbreken",
    "Disconnecting": "Verbinding verbreken",
    "Disconnecting...": "Verbinding verbreken...",
    "Display settings": "Scherminstellingen",
    "Don't have an account yet?": "Geen account?",
    "Don't send": "Niet verzenden",
    "Don't show again": "Niet meer weergeven",
    "Don't show this again": "Niet meer weergeven",
    "Don't want to wait? Upgrade now": "Why wait? Upgrade now",
    "Done": "Gereed",
    "Email address": "E-mailadres",
    "Email confirmation": "E-mailbevestiging",
    "Email sent": "E-mail verzonden",
    "Enjoy your unlimited data volume.<br/>Remaining days: {0}": "Geniet van uw onbeperkte gegevensvolume.<br/>Resterende dagen: {0}",
    "Enter a valid email address first": "Voer een geldig e-mailadres in",
    "Enter a valid password": "Voer een geldig wachtwoord in",
    "Enter a valid verification code.": "Voer een geldige verificatiecode in.",
    "Enter your Mac password and choose <span class=\"privacyDescription__text__bold\">Always allow</span> in the next window to continue.": "Voer in het volgende scherm uw Mac-wachtwoord in en kies <span class=\"privacyDescription__text__bold\">Altijd toestaan</span> om door te gaan.",
    "Establishing connection...": "Verbinding maken...",
    "Exit": "Afsluiten",
    "Failed to connect to the service.": "Verbinding maken met apparaat mislukt",
    "Failed to connect using UDP protocol. Retrying with TCP protocol.": "Verbinding maken via UDP-protocol mislukt. Wordt opnieuw geprobeerd via het TCP-protocol.",
    "Failed to establish the VPN connection. Please try again.": "Kan geen verbinding maken. Probeer het opnieuw.",
    "Failed to purchase. Please try again later.": "Aankoop mislukt. Probeer het later opnieuw.",
    "Fatal error.": "Kritieke fout.",
    "Fill in this field.": "Vul dit veld in.",
    "For security reasons your account has been blocked. Please access Avira Connect to unblock it.": "Uw account is vergrendeld. U kunt uw account eenvoudig ontgrendelen via Avira Connect.",
    "Forgot your password?": "Wachtwoord vergeten?",
    "Get 3 Months Free": "Pro kopen voor 3 maanden",
    "Get Pro": "Pro kopen",
    "Get all the protection you need.": "Krijg alle bescherming die u nodig hebt.",
    "Get for this occasion your three-month free trial. Thank you very much for helping us in beta stage by using and testing our product.": "Ontvang 3 maanden gratis. Bedankt voor uw gebruik van ons product en uw hulp bij het verbeteren.",
    "Get started": "Aan de slag",
    "Get support": "Ondersteuning krijgen",
    "Get unlimited data volume now.": "Krijg nu onbeperkte toegang.",
    "Help Avira improve its products and services by automatically sending daily anonymous diagnostic and usage data.": "Help Avira om onze producten en services te verbeteren door dagelijks automatisch anonieme diagnose- en gebruiksgegevens te verzenden.",
    "Help us improve": "Help ons te verbeteren",
    "I entered a wrong email": "Ik heb het verkeerde e-mailadres opgegeven",
    "IPSec traffic is blocked. Please contact your network administrator.": "IPSec-verkeer is geblokkeerd. Neem contact op met uw netwerkbeheerder.",
    "In case of technical issues you can collect diagnostic data and send a report to the developers.": "Having technical issues? We're here for you. Send us a report and we'll do the rest.",
    "Invalid credentials": "Ongeldige inloggegevens",
    "Launch at system start": "Activeren bij systeemstart",
    "Length of subscription: 1 month/1 year<br><br> \n                    Price of subscription: 4,95€ per month (Mac only) – 7,95€ per month (all devices) – 59,95€ per year (all devices).<br><br>\n                    \n                    Payment will be charged to iTunes Account at confirmation of purchase. Subscription automatically renews unless auto-renew is turned off at least 24-hours before the end of the current period. Account will be charged for renewal within 24-hours prior to the end of the current period, and identify the cost of the renewal. Subscriptions may be managed by the user and auto-renewal may be turned off by going to the user's Account Settings after purchase. Any unused portion of a free trial period, if offered, will be forfeited when the user purchases a subscription to that publication, where applicable.": "Duur van abonnement: 1 maand/1 jaar - Prijs van abonnement: € 4,95 per maand (alleen Mac) - € 7,95 per maand (alle apparaten) - € 59,95 per jaar (alle apparaten). De betaling wordt in rekening gebracht op het iTunes-account, bij bevestiging van de aankoop. Het abonnement wordt automatisch verlengd tenzij automatisch verlengen ten minste 24 uur voor het einde van de huidige periode is uitgeschakeld. Het bedrag voor het verlengen wordt binnen 24 uur voor het einde van de huidige periode in rekening gebracht op het account en de kosten van het verlengen worden aangegeven. Abonnementen kunnen worden beheerd door de gebruiker en automatisch verlengen kan worden uitgezet na aankoop via de accountinstellingen van de gebruiker. Elk ongebruikte deel van de gratis proefperiode, indien aangeboden, komt te vervallen wanneer de gebruiker een abonnement koopt op die publicatie, waar van toepassing.",
    "Light": "Licht",
    "Light theme": "Licht thema",
    "Log in": "Aanmelden",
    "Log out": "Afmelden",
    "Logging in...": "Aanmelden…",
    "Monthly:": "Maandelijks:",
    "My Account": "Mijn account",
    "New to Avira?": "Bent u nieuw bij Avira?",
    "No Wi-Fi network saved yet for automatic VPN connection. Once saved, they will automatically appear here and can be configured.": "Nog geen wifinetwerk opgeslagen voor een automatisch VPN-verbinding. Een opgeslagen netwerk wordt hier automatisch weergegeven en kan worden geconfigureerd.",
    "No network available.": "Geen netwerk beschikbaar",
    "No purchase found. Nothing to restore.": "Geen aankoop gevonden. Niets te herstellen.",
    "Not Now": "Niet nu",
    "Not now": "Niet nu",
    "Okay": "OK",
    "On all my devices": "Op al mijn apparaten",
    "On my Mac": "Op mijn Mac",
    "On my PC": "Op mijn pc",
    "Oops. Sorry, there was a error in the authentication process. Try again later or contact Support.": "Oeps. Er is een fout opgetreden in het authenticatieproces. Probeer het later opnieuw of neem contact op met Ondersteuning.",
    "Oops. Sorry, there was a error in the registration process. Try again later or contact Support.": "Oeps. Er is een fout opgetreden in het registratieproces. Probeer het later opnieuw of neem contact op met Ondersteuning.",
    "Oops. Sorry, this email address is already registered with another account.": "Oeps. Dit e-mailadres is al geregistreerd met een ander account.",
    "Password": "Wachtwoord",
    "Please register if you want to purchase Phantom VPN Pro on all your devices": "Registreer om Phantom VPN Pro voor al uw apparaten te kopen.",
    "Privacy is important to us": "Privacy is belangrijk voor ons",
    "Privacy policy": "Privacybeleid",
    "Purchasing...": "Wordt gekocht...",
    "Quit": "Afsluiten",
    "Rate 5 stars": "Waarderen met 5 sterren",
    "Rate Avira Phantom VPN": "Avira Phantom VPN waarderen",
    "Register": "Registreren",
    "Register for an Avira account<br>and get your 30 day free trial": "Registreer voor een Avira-account<br>en probeer het 30 dagen gratis",
    "Registering...": "Registreren...",
    "Renew now": "Nu verlengen",
    "Resend email": "E-mail opnieuw verzenden",
    "Restore purchase": "Aankoop herstellen",
    "Restoring...": "Herstellen...",
    "Secure my connection": "Mijn verbinding beveiligen",
    "Securing your connection": "Uw verbinding beveiligen",
    "Select virtual location": "Virtuele locatie selecteren",
    "Send": "Verzenden",
    "Send diagnostic data": "Diagnostische data verzenden",
    "Send feedback": "Feedback verzenden",
    "Send report": "Create report",
    "Settings": "Instellingen",
    "Special Offer": "Speciale aanbieding",
    "Subscription terms": "Abonnementsvoorwaarden",
    "Tap Adapter not present or disabled.": "TAP-adapter niet aanwezig of uitgeschakeld",
    "Tell us what you think. Your feedback helps us improve.": "Laat ons weten wat u denkt. Met uw feedback kunnen we onze service verbeteren.",
    "Terms and conditions": "Algemene voorwaarden",
    "Test Pro for free": "Test Pro gratis",
    "The amount of data that you consume. We do this to help calculate the costs of providing our VPN infrastructure": "De hoeveelheid gegevens die u verbruikt. We doen dit om een berekening te kunnen maken van de kosten voor het leveren van onze VPN-infrastructuur.",
    "The app's color theme will use the operating system setting unless you choose a different theme.": "Het kleurenthema van de app gebruikt de instellingen van het besturingssysteem, tenzij u een ander thema selecteert.",
    "The app's color theme will use the operating system setting.": "Het kleurenthema van de app gebruikt de instellingen van het besturingssysteem.",
    "The connection is not private": "De verbinding is niet privé",
    "The connection is private": "De verbinding is privé",
    "The selected location is invalid. Please check the network connection.": "De geselecteerde locatie is ongeldig. Controleer de netwerkverbinding.",
    "Three-Month free trial, then only <span>{0}</span> monthly": "3-maanden gratis proefversie, daarna slechts <span>{0}</span> per maand",
    "To encrypt your internet connection, Phantom VPN uses the neagent system service. It needs your permission to access the Keychain.": "Phantom VPN gebruikt de neagent-systeemservice om uw internetverbinding te versleutelen. We hebben uw toestemming nodig om dit in te stellen.",
    "To try the best experience we will give you unlimited data volume for 30 days for free.": "Om de beste ervaring uit te proberen, geven we u 30 dagen gratis onbeperkt gegevensvolume.",
    "Traffic limit reached": "Verkeerslimiet bereikt",
    "Trial": "Proefversie",
    "Unknown": "Onbekend",
    "Unknown error.": "Onbekende fout.",
    "Unlimited": "Onbeperkt",
    "Unlimited traffic": "Onbeperkt verkeer",
    "Unlimited traffic on: Windows, macOS, iOS, Android, Google Chrome": "Onbeperkt verkeer op: Windows: macOS, iOS, Android Google Chrome",
    "Use fastest protocol when available": "Snelste protocol gebruiken indien beschikbaar",
    "Use system settings": "Systeeminstelling gebruiken",
    "Verification code": "Verificatiecode",
    "Virtual location set to <a> {0} </a>": "Virtuele locatie: <a> {0} </a>",
    "Virtual location: {0}": "Virtuele locatie: {0}",
    "We sent a verification code to your phone number": "We hebben een verificatiecode verzonden naar uw telefoonnummer.",
    "We want to be fully transparent about the data you agree to share with us. To provide you a smooth VPN experience we need a minimum set of data:": "We willen volledige transparantie over de gegevens die u met ons deelt. We hebben in ieder geval de volgende gegevens nodig voor een probleemloze VPN-ervaring:",
    "Whether you are a free or a paid user. It's important for our communications to be able to differentiate the two": "Of u de gratis of betaalde versie gebruikt. Het is belangrijk voor onze communicatie om een onderscheid tussen de twee te maken.",
    "Yearly:": "Jaarlijks:",
    "You will be disconnected in {0} seconds.": "De verbinding wordt over {0} seconden verbroken.",
    "Your connection is secure": "Uw verbinding is veilig",
    "Your connection is secure.": "Uw verbinding is veilig",
    "Your connection is unsecure": "Uw verbinding is onveilig",
    "Your internet connection was protected from prying eyes. Is this worth 5 stars?": "Uw internetverbinding is beschermd tegen nieuwsgierige ogen. Is dit 5 sterren waard?",
    "Your license will expire soon.<br/>Remaining days: {0}": "Uw licentie verloopt binnenkort.<br/>Resterende dagen: {0}",
    "Your password must contain at least 8 characters, one digit, and one uppercase letter.": "Uw wachtwoord moet minimaal 8 tekens bevatten, waarvan één cijfer en één hoofdletter.",
    "default": "standaard",
    "https://www.research.net/r/HLV5Q2H?": "https://www.research.net/r/HLV5Q2H?",
    "https://www.research.net/r/J5H53HC?": "https://www.research.net/r/J5H53HC?",
    "or get 500 MB if you <a> register </a>": "of ontvang 500 MB als u zich <a>registreert</a>",
    "{0} out of {1} daily secured traffic": "{0} van {1} dagelijks beveiligd verkeer",
    "{0} out of {1} monthly secured traffic": "{0} van {1} maandelijks beveiligd verkeer",
    "{0} out of {1} weekly secured traffic": "{0} van {1} wekelijks beveiligd verkeer",
    "{0} secured traffic": "{0} beveiligd verkeer",
    "%": "%",
    "{0} used": "{0} gebruikt"
  });
  /* jshint +W100 */
}]);

},{}],66:[function(require,module,exports){
"use strict";

angular.module('LauncherApp').run(['gettextCatalog', function (gettextCatalog) {
  /* jshint -W100 */
  gettextCatalog.setStrings('pt_BR', {
    "0 bytes": "0 bytes",
    "<span class=\"privacyDescription__text__bold\">We don't log your online activity or any information that can link you to any action</span>, such as downloading a file, or visiting a particular website.": "<span class=\"privacyDescription__text__bold\">Não registramos sua atividade online ou qualquer informação que possa vincular você a qualquer ação</span>, como baixar um arquivo ou visitar um site específico.",
    "A new driver has been installed, please restart your computer to complete the installation": "Reinicie o computador antes de proteger sua conexão.",
    "About": "Sobre",
    "Access": "Continuar",
    "Account details": "Detalhes da Conta",
    "Agree and continue": "Concordar e continuar",
    "All free slots are currently taken.": "No slots free currently.",
    "Already have an account?": "Já tem uma conta?",
    "Always Allow": "Sempre permitir",
    "Auto-connect VPN": "Conexão automática do VPN",
    "Auto-connect VPN for Wi-Fi networks": "Conexão automática do VPN às redes Wi-Fi",
    "Automatically secure untrusted Wi-Fi networks": "Proteja automaticamente as redes Wi-Fi não confiáveis",
    "Avira Phantom VPN Pro released!": "O Avira Phantom VPN Pro chegou!",
    "Back": "Voltar",
    "Block all internet traffic if VPN connection drops": "Bloquear todo o tráfego de Internet se a conexão VPN cair",
    "Block intrusive ads": "Bloquear anúncios intrusivos",
    "Block malicious sites and content": "Bloquear sites e conteúdos maliciosos",
    "Buy": "Comprar",
    "Buy unlimited traffic": "Comprar tráfego ilimitado",
    "By proceeding, you are accepting the <a href=\"#\" ng-click=\"openEula()\">End User License Agreement (EULA)</a>, and the <a href=\"#\" ng-click=\"openTermsAndConditions()\">Terms and Conditions</a>. Avira is fulfilling its duties to provide information in accordance with Articles 13 and 14 of the General Data Protection Regulation (GDPR) with the contents of the Privacy Policy and access thereto. You can find our Privacy Policy here: <a href=\"#\" ng-click=\"openPrivacyAndPolicy()\">{{getPrivacyPolicyLink()}}</a>": "Ao continuar, você aceita o <a href=\"#\" ng-click=\"openEula()\">Contrato de Licença de Usuário Final (EULA)</a> e os <a href=\"#\" ng-click=\"openTermsAndConditions()\">Termos e Condições</a>. A Avira está cumprindo sua obrigação de fornecer informações de acordo com os artigos 13 e 14 do Regulamento Geral sobre a Proteção de Dados (RGPD) com o conteúdo da Política de Privacidade e acesso à mesma. Você pode ler nossa Política de Privacidade aqui: <a href=\"#\" ng-click=\"openPrivacyAndPolicy()\">{{getPrivacyPolicyLink()}}</a>",
    "Cancel": "Cancelar",
    "Cannot resolve host address.": "Não é possível resolver o endereço do host.",
    "Change later in Settings": "Alterar depois nas configurações",
    "Check your inbox to confirm it's you. Make sure to look into your spam and junk folders as well.": "Verifique sua caixa de entrada para confirmar seu email. Lembre-se de olhar nas pastas de spam e lixo também.",
    "Choose your color theme": "Escolha o seu tema de cores",
    "Code": "Código",
    "Collect diagnostic data": "Enviar",
    "Connect": "Conectar",
    "Connecting": "Conectando",
    "Connecting...": "Conectando...",
    "Connection to server lost.": "Conexão do servidor perdida.",
    "Continue": "Continuar",
    "Dark": "Escuro",
    "Dark theme": "Tema escuro",
    "Disconnect": "Desconectar",
    "Disconnecting": "Desconectando",
    "Disconnecting...": "Desconectando...",
    "Display settings": "Configurações de exibição",
    "Don't have an account yet?": "Não tem conta?",
    "Don't send": "Não enviar",
    "Don't show again": "Não exibir novamente",
    "Don't show this again": "Não exibir isto novamente",
    "Don't want to wait? Upgrade now": "Why wait? Upgrade now",
    "Done": "Concluído",
    "Email address": "Endereço de e-mail",
    "Email confirmation": "Confirmação de email",
    "Email sent": "Email enviado",
    "Enjoy your unlimited data volume.<br/>Remaining days: {0}": "Aproveite seu volume de dados ilimitado.<br/>Dias restantes: {0}",
    "Enter a valid email address first": "Insira um endereço de email válido",
    "Enter a valid password": "Insira uma senha válida",
    "Enter a valid verification code.": "Insira um código de verificação válido.",
    "Enter your Mac password and choose <span class=\"privacyDescription__text__bold\">Always allow</span> in the next window to continue.": "Insira sua senha do Mac e, na caixa de diálogo seguinte, selecione <span class=\"privacyDescription__text__bold\">Sempre permitir</span> para continuar.",
    "Establishing connection...": "Estabelecendo conexão...",
    "Exit": "Sair",
    "Failed to connect to the service.": "Falha ao conectar ao serviço.",
    "Failed to connect using UDP protocol. Retrying with TCP protocol.": "Falha ao conectar com o protocolo UDP. Tentar novamente com o protocolo TCP.",
    "Failed to establish the VPN connection. Please try again.": "Não foi possível conectar. Tente novamente.",
    "Failed to purchase. Please try again later.": "Falha na compra. Tente novamente mais tarde.",
    "Fatal error.": "Erro fatal.",
    "Fill in this field.": "Preencha este campo.",
    "For security reasons your account has been blocked. Please access Avira Connect to unblock it.": "Sua conta foi bloqueada. Para desbloqueá-la, basta acessar o Avira Connect.",
    "Forgot your password?": "Esqueceu sua senha?",
    "Get 3 Months Free": "Obter Pro por 3 meses",
    "Get Pro": "Obter Pro",
    "Get all the protection you need.": "Tenha toda a proteção que você precisa.",
    "Get for this occasion your three-month free trial. Thank you very much for helping us in beta stage by using and testing our product.": "Obtenha a oferta especial de três meses gratuitos. Muito obrigado por experimentar nosso produto e ajudar a aprimorá-lo.",
    "Get started": "Começar",
    "Get support": "Obter suporte",
    "Get unlimited data volume now.": "Obtenha acesso ilimitado agora.",
    "Help Avira improve its products and services by automatically sending daily anonymous diagnostic and usage data.": "Ajude a Avira a melhorar os produtos e serviços enviando automaticamente dados de diagnóstico e de uso anônimos diários.",
    "Help us improve": "Ajude-nos a melhorar",
    "I entered a wrong email": "Inseri um endereço de email errado",
    "IPSec traffic is blocked. Please contact your network administrator.": "Tráfego de IPSec bloqueado. Entre em contato com o administrador de rede.",
    "In case of technical issues you can collect diagnostic data and send a report to the developers.": "Having technical issues? We're here for you. Send us a report and we'll do the rest.",
    "Invalid credentials": "Credenciais inválidas",
    "Launch at system start": "Iniciar com a inicialização do sistema",
    "Length of subscription: 1 month/1 year<br><br> \n                    Price of subscription: 4,95€ per month (Mac only) – 7,95€ per month (all devices) – 59,95€ per year (all devices).<br><br>\n                    \n                    Payment will be charged to iTunes Account at confirmation of purchase. Subscription automatically renews unless auto-renew is turned off at least 24-hours before the end of the current period. Account will be charged for renewal within 24-hours prior to the end of the current period, and identify the cost of the renewal. Subscriptions may be managed by the user and auto-renewal may be turned off by going to the user's Account Settings after purchase. Any unused portion of a free trial period, if offered, will be forfeited when the user purchases a subscription to that publication, where applicable.": "Duração da assinatura: 1 mês/1 ano - Preço da assinatura: 4,95 € por mês (somente Mac) - 7,95 € por mês (todos os dispositivos) - 59,95 € por ano (todos os dispositivos). O pagamento será cobrado na conta iTunes na confirmação da compra. A assinatura será renovada automaticamente, a menos que seja desativada pelo menos 24 horas antes do fim do período atual. A conta será cobrada pela renovação dentro de 24 horas, antes do fim do período atual, e identificará o custo da renovação. As assinaturas podem ser gerenciadas pelo usuário e a renovação automática pode ser desativada acessando as Configurações da conta do usuário após a compra. Qualquer período de avaliação gratuita não utilizado, se oferecido, será perdido quando o usuário comprar uma assinatura desta publicação, onde aplicável.",
    "Light": "Claro",
    "Light theme": "Tema claro",
    "Log in": "Faça logon",
    "Log out": "Fazer logout",
    "Logging in...": "Fazendo logon...",
    "Monthly:": "Mensal:",
    "My Account": "Minha conta",
    "New to Avira?": "Você é novo no Avira?",
    "No Wi-Fi network saved yet for automatic VPN connection. Once saved, they will automatically appear here and can be configured.": "Nenhuma rede Wi-Fi salva para conexão VPN automática. Uma vez salvas, elas serão exibidas aqui e poderão ser configuradas.",
    "No network available.": "Nenhuma rede disponível.",
    "No purchase found. Nothing to restore.": "Nenhuma compra encontrada. Nada para restaurar.",
    "Not Now": "Agora não",
    "Not now": "Agora não",
    "Okay": "OK",
    "On all my devices": "Em todos meus dispositivos",
    "On my Mac": "Em meu Mac",
    "On my PC": "Em meu PC",
    "Oops. Sorry, there was a error in the authentication process. Try again later or contact Support.": "Oops. Desculpe, ocorreu um erro no processo de autenticação. Tente novamente mais tarde ou entre em contato com o suporte.",
    "Oops. Sorry, there was a error in the registration process. Try again later or contact Support.": "Oops. Desculpe, ocorreu um erro no processo de registro. Tente novamente mais tarde ou entre em contato com o suporte.",
    "Oops. Sorry, this email address is already registered with another account.": "Oops. Desculpe, este endereço de email já registrado em outra conta.",
    "Password": "Senha",
    "Please register if you want to purchase Phantom VPN Pro on all your devices": "Registre-se para comprar o Phantom VPN Pro para todos os seus dispositivos.",
    "Privacy is important to us": "A privacidade é importante para nós",
    "Privacy policy": "Política de Privacidade",
    "Purchasing...": "Comprando...",
    "Quit": "Sair",
    "Rate 5 stars": "Classificar com 5 estrelas",
    "Rate Avira Phantom VPN": "Classifique o Avira Phantom VPN",
    "Register": "Registrar-se",
    "Register for an Avira account<br>and get your 30 day free trial": "Registre-se em uma conta Avira<br>e ganhe 30 dias de avaliação gratuita",
    "Registering...": "Registrando...",
    "Renew now": "Renove agora",
    "Resend email": "Reenviar email",
    "Restore purchase": "Restaurar a compra",
    "Restoring...": "Restaurando...",
    "Secure my connection": "Proteger minha conexão",
    "Securing your connection": "Tornar sua conexão segura",
    "Select virtual location": "Selecionar localização virtual",
    "Send": "Enviar",
    "Send diagnostic data": "Enviar dados de diagnóstico",
    "Send feedback": "Enviar feedback",
    "Send report": "Create report",
    "Settings": "Configurações",
    "Special Offer": "Oferta especial",
    "Subscription terms": "Termos da assinatura",
    "Tap Adapter not present or disabled.": "Adaptador TAP não existe ou está desativado.",
    "Tell us what you think. Your feedback helps us improve.": "Diga-nos o que você pensa. Seus comentários ajudam-nos a melhorar.",
    "Terms and conditions": "Termos e condições",
    "Test Pro for free": "Teste gratuito da versão Pro",
    "The amount of data that you consume. We do this to help calculate the costs of providing our VPN infrastructure": "A quantidade de dados que você consome. Fazemos isto para ajudar a calcular os custos de fornecimento da nossa infraestrutura de VPN.",
    "The app's color theme will use the operating system setting unless you choose a different theme.": "As cores do app serão iguais a configuração do sistema, a não ser que escolha um tema diferente.",
    "The app's color theme will use the operating system setting.": "As cores do app serão iguais a configuração do sistema operacional.",
    "The connection is not private": "A conexão não é privada",
    "The connection is private": "A conexão é privada",
    "The selected location is invalid. Please check the network connection.": "A localização selecionada é inválida. Verifique a conexão de rede.",
    "Three-Month free trial, then only <span>{0}</span> monthly": "Avaliação gratuita por três meses, depois <span>{0}</span> por mês",
    "To encrypt your internet connection, Phantom VPN uses the neagent system service. It needs your permission to access the Keychain.": "Para criptografar sua conexão com a Internet, o Phantom VPN usa o serviço de sistema neagent. Precisamos da sua permissão para configurá-lo.",
    "To try the best experience we will give you unlimited data volume for 30 days for free.": "Para que você tenha a melhor experiência, forneceremos um volume de dados ilimitado por 30 dias gratuitamente.",
    "Traffic limit reached": "Limite de tráfego atingido",
    "Trial": "Versão de teste",
    "Unknown": "Desconhecido",
    "Unknown error.": "Erro desconhecido.",
    "Unlimited": "Ilimitado",
    "Unlimited traffic": "Tráfego ilimitado",
    "Unlimited traffic on: Windows, macOS, iOS, Android, Google Chrome": "Tráfego ilimitado: Windows, macOS, iOS, Android, Google Chrome",
    "Use fastest protocol when available": "Usar o protocolo mais rápido se disponível",
    "Use system settings": "Usar configuração do sistema",
    "Verification code": "Código de verificação",
    "Virtual location set to <a> {0} </a>": "Localização virtual: <a> {0} </a>",
    "Virtual location: {0}": "Localização virtual: {0}",
    "We sent a verification code to your phone number": "Enviamos um código de verificação para o seu número de telefone.",
    "We want to be fully transparent about the data you agree to share with us. To provide you a smooth VPN experience we need a minimum set of data:": "Queremos ser completamente transparentes sobre os dados que você concorda em compartilhar conosco. Para fornecer uma experiência de VPN tranquila, precisamos de um conjunto mínimo de dados:",
    "Whether you are a free or a paid user. It's important for our communications to be able to differentiate the two": "Se você é um usuário gratuito ou um assinante. É importante para nossas comunicações poder diferenciar os dois.",
    "Yearly:": "Anual:",
    "You will be disconnected in {0} seconds.": "Você será desconectado em {0} segundos.",
    "Your connection is secure": "Sua conexão é segura",
    "Your connection is secure.": "Sua conexão é segura",
    "Your connection is unsecure": "Sua conexão é insegura",
    "Your internet connection was protected from prying eyes. Is this worth 5 stars?": "A sua conexão com a Internet foi protegida contra olhares curiosos. Isto vale 5 estrelas?",
    "Your license will expire soon.<br/>Remaining days: {0}": "Sua licença expirará em breve.<br/>Dias restantes: {0}",
    "Your password must contain at least 8 characters, one digit, and one uppercase letter.": "Sua senha deve conter pelo menos 8 caracteres, um dígito e uma letra maiúscula.",
    "default": "padrão",
    "https://www.research.net/r/HLV5Q2H?": "https://www.research.net/r/HLV5Q2H?",
    "https://www.research.net/r/J5H53HC?": "https://www.research.net/r/J5H53HC?",
    "or get 500 MB if you <a> register </a>": "ou obtenha 500 MB se você se <a> registrar </a>",
    "{0} out of {1} daily secured traffic": "{0} dos {1} de tráfego protegido por dia",
    "{0} out of {1} monthly secured traffic": "{0} dos {1} de tráfego protegido por mês",
    "{0} out of {1} weekly secured traffic": "{0} dos {1} de tráfego protegido por semana",
    "{0} secured traffic": "{0} tráfego seguro",
    "%": "%",
    "{0} used": "{0} usado"
  });
  /* jshint +W100 */
}]);

},{}],67:[function(require,module,exports){
"use strict";

angular.module('LauncherApp').run(['gettextCatalog', function (gettextCatalog) {
  /* jshint -W100 */
  gettextCatalog.setStrings('ru_RU', {
    "0 bytes": "0 байт",
    "<span class=\"privacyDescription__text__bold\">We don't log your online activity or any information that can link you to any action</span>, such as downloading a file, or visiting a particular website.": "<span class=\"privacyDescription__text__bold\">Мы не храним данные о ваших действиях в сети или сведения, позволяющие связать вас с любыми действиями</span>, например загрузкой файла или посещением определенного веб-сайта.",
    "A new driver has been installed, please restart your computer to complete the installation": "Перед защитой подключения перезагрузите компьютер.",
    "About": "О программе",
    "Access": "Продолжить",
    "Account details": "Сведения об учетной записи",
    "Agree and continue": "Принять и продолжить",
    "All free slots are currently taken.": "No slots free currently.",
    "Already have an account?": "Уже есть учетная запись?",
    "Always Allow": "Всегда разрешать",
    "Auto-connect VPN": "Автоподкл. VPN",
    "Auto-connect VPN for Wi-Fi networks": "Автоподкл. VPN в сетях Wi-Fi",
    "Automatically secure untrusted Wi-Fi networks": "Автоматическая защита в небезопасных сетях Wi-Fi",
    "Avira Phantom VPN Pro released!": "Добавлена программа Avira Phantom VPN Pro!",
    "Back": "Назад",
    "Block all internet traffic if VPN connection drops": "Блокировка трафика при обрыве VPN-соединения",
    "Block intrusive ads": "Блокировать назойливую рекламу",
    "Block malicious sites and content": "Блокировать вредоносные сайты и содержимое",
    "Buy": "Купить",
    "Buy unlimited traffic": "Приобрести неограниченный трафик",
    "By proceeding, you are accepting the <a href=\"#\" ng-click=\"openEula()\">End User License Agreement (EULA)</a>, and the <a href=\"#\" ng-click=\"openTermsAndConditions()\">Terms and Conditions</a>. Avira is fulfilling its duties to provide information in accordance with Articles 13 and 14 of the General Data Protection Regulation (GDPR) with the contents of the Privacy Policy and access thereto. You can find our Privacy Policy here: <a href=\"#\" ng-click=\"openPrivacyAndPolicy()\">{{getPrivacyPolicyLink()}}</a>": "Продолжая, вы принимаете <a href=\"#\" ng-click=\"openEula()\"> Лицензионное соглашение с конечным пользователем (EULA)</a> и <a href=\"#\" ng-click=\"openTermsAndConditions()\"> Коммерческие условия</a>. Avira выполняет обязательства по передаче информации в соответствии со Статьями 13 и 14 Общего регламента по защите данных (GDPR), предоставляя содержание Политики конфиденциальности и доступ к этому документу. С нашей Политикой конфиденциальности вы можете ознакомиться здесь: <a href=\"#\" ng-click=\"openPrivacyAndPolicy()\">{{getPrivacyPolicyLink()}}</a>",
    "Cancel": "Отмена",
    "Cannot resolve host address.": "Не удалось определить адрес узла.",
    "Change later in Settings": "Изменить позже в Настройках",
    "Check your inbox to confirm it's you. Make sure to look into your spam and junk folders as well.": "Проверьте входящую почту, чтобы подтвердить свою личность. Кроме того, проверьте папки для спама.",
    "Choose your color theme": "Выбрать тему",
    "Code": "Код",
    "Collect diagnostic data": "Отправить",
    "Connect": "Соединить",
    "Connecting": "Соединение",
    "Connecting...": "Соединение...",
    "Connection to server lost.": "Прервано соединение с сервером.",
    "Continue": "Продолжить",
    "Dark": "Темная",
    "Dark theme": "Темная тема",
    "Disconnect": "Отключиться",
    "Disconnecting": "Отключение",
    "Disconnecting...": "Отключение...",
    "Display settings": "Настройки экрана",
    "Don't have an account yet?": "Нет учетной записи?",
    "Don't send": "Не отправлять",
    "Don't show again": "Больше не показывать",
    "Don't show this again": "Больше не показывать",
    "Don't want to wait? Upgrade now": "Why wait? Upgrade now",
    "Done": "Готово",
    "Email address": "Адрес электронной почты",
    "Email confirmation": "Подтверждение адреса эл. почты",
    "Email sent": "Oтправлено",
    "Enjoy your unlimited data volume.<br/>Remaining days: {0}": "Оцените доступ к неограниченному объему данных.<br/>Осталось дней: {0}",
    "Enter a valid email address first": "Введите действующий адрес электронной почты",
    "Enter a valid password": "Введите допустимый пароль",
    "Enter a valid verification code.": "Введите допустимый код проверки.",
    "Enter your Mac password and choose <span class=\"privacyDescription__text__bold\">Always allow</span> in the next window to continue.": "Чтобы продолжить, введите пароль для Mac и в следующем окне выберите <span class=\"privacyDescription__text__bold\">Всегда разрешать</span>.",
    "Establishing connection...": "Установка соединения...",
    "Exit": "Выход",
    "Failed to connect to the service.": "Не удалось установить соединение со службой.",
    "Failed to connect using UDP protocol. Retrying with TCP protocol.": "Ошибка соединения с помощью протокола UDP. Соединение с помощью протокола TCP.",
    "Failed to establish the VPN connection. Please try again.": "Ошибка соединения. Повторите попытку.",
    "Failed to purchase. Please try again later.": "Покупка не завершена. Повторите попытку позже.",
    "Fatal error.": "Глобальная ошибка.",
    "Fill in this field.": "Заполните это поле.",
    "For security reasons your account has been blocked. Please access Avira Connect to unblock it.": "Учетная запись заблокирована. Вы можете ее разблокировать на панели Avira Connect.",
    "Forgot your password?": "Забыли пароль?",
    "Get 3 Months Free": "Купить Pro на 3 месяца",
    "Get Pro": "Купить Pro",
    "Get all the protection you need.": "Получите всю необходимую защиту.",
    "Get for this occasion your three-month free trial. Thank you very much for helping us in beta stage by using and testing our product.": "Получите бесплатную пробную версию на 3 месяца. Благодарим за использование и помощь в улучшении программ.",
    "Get started": "Начать",
    "Get support": "Поддержка",
    "Get unlimited data volume now.": "Получите неограниченный доступ.",
    "Help Avira improve its products and services by automatically sending daily anonymous diagnostic and usage data.": "Помогите Avira в улучшении программ и служб, включив автоматическую ежедневную отправку анонимных данных использования и диагностики.",
    "Help us improve": "Помогите нам стать лучше",
    "I entered a wrong email": "Я указал(а) неверный адрес эл. почты",
    "IPSec traffic is blocked. Please contact your network administrator.": "Трафик IPSec заблокирован. Обратитесь к администратору сети.",
    "In case of technical issues you can collect diagnostic data and send a report to the developers.": "Having technical issues? We're here for you. Send us a report and we'll do the rest.",
    "Invalid credentials": "Неверные учетные данные",
    "Launch at system start": "Автозагрузка при запуске системы",
    "Length of subscription: 1 month/1 year<br><br> \n                    Price of subscription: 4,95€ per month (Mac only) – 7,95€ per month (all devices) – 59,95€ per year (all devices).<br><br>\n                    \n                    Payment will be charged to iTunes Account at confirmation of purchase. Subscription automatically renews unless auto-renew is turned off at least 24-hours before the end of the current period. Account will be charged for renewal within 24-hours prior to the end of the current period, and identify the cost of the renewal. Subscriptions may be managed by the user and auto-renewal may be turned off by going to the user's Account Settings after purchase. Any unused portion of a free trial period, if offered, will be forfeited when the user purchases a subscription to that publication, where applicable.": "Срок действия подписки: 1 мес./1 г. Цена подписки: 4,95 евро/мес. (только Mac), 7,95 евро/мес. (все устройства), 59,95 евро/г. (все устройства). Оплата будет списана из учетной записи iTunes при подтверждении заказа. Подписка продлевается автоматически, если автоматическое продление не было отключено минимум за 24 часа до окончания срока ее действия. Оплата за продление будет списана из учетной записи в течение последних 24 часов текущего периода, а также будет определена стоимость продления. Пользователи могут продлевать подписку. Автоматическое продление можно отключить, перейдя после покупки в настройки учетной записи пользователя. Неиспользованное время бесплатного пробного периода, если он был предложен, будет утрачено в момент приобретения пользователем подписки при соответствующих условиях.",
    "Light": "Светлая",
    "Light theme": "Светлая тема",
    "Log in": "Войти",
    "Log out": "Выход",
    "Logging in...": "Выполняется вход...",
    "Monthly:": "На месяц:",
    "My Account": "Моя учетная запись",
    "New to Avira?": "Раньше не пользовались Avira?",
    "No Wi-Fi network saved yet for automatic VPN connection. Once saved, they will automatically appear here and can be configured.": "Нет сохраненных сетей Wi-Fi, в которых можно использовать автоподключение VPN. Сохраненные сети появятся здесь, после чего их можно будет настроить.",
    "No network available.": "Сеть недоступна.",
    "No purchase found. Nothing to restore.": "Покупка не найдена. Нечего восстанавливать.",
    "Not Now": "Не сейчас",
    "Not now": "Не сейчас",
    "Okay": "ОК",
    "On all my devices": "На всех моих устройствах",
    "On my Mac": "На Mac",
    "On my PC": "На ПК",
    "Oops. Sorry, there was a error in the authentication process. Try again later or contact Support.": "Ой! К сожалению, при проверке подлинности произошла ошибка. Попробуйте позже или обратитесь в Поддержку.",
    "Oops. Sorry, there was a error in the registration process. Try again later or contact Support.": "Ой! К сожалению, в процессе регистрации произошла ошибка. Попробуйте позже или обратитесь в Поддержку.",
    "Oops. Sorry, this email address is already registered with another account.": "Ой! К сожалению, этот адрес электронной почты уже зарегистрирован.",
    "Password": "Пароль",
    "Please register if you want to purchase Phantom VPN Pro on all your devices": "Зарегистрируйтесь и приобретите Phantom VPN Pro для всех устройств.",
    "Privacy is important to us": "Конфиденциальность — это важно",
    "Privacy policy": "Политика конфиденциальности",
    "Purchasing...": "Выполняется покупка...",
    "Quit": "Выйти",
    "Rate 5 stars": "Оценить в 5 звезд",
    "Rate Avira Phantom VPN": "Оцените Avira Phantom VPN",
    "Register": "Регистрация",
    "Register for an Avira account<br>and get your 30 day free trial": "Зарегистрируйте учетную запись Avira<br>и получите 30-дневную бесплатную пробную версию",
    "Registering...": "Регистрация...",
    "Renew now": "Продлить",
    "Resend email": "Отправить еще раз",
    "Restore purchase": "Восстановить приложение",
    "Restoring...": "Восстановление...",
    "Secure my connection": "Oбезопасить соединениe",
    "Securing your connection": "Защита соединения",
    "Select virtual location": "Выберите виртуальное местоположение",
    "Send": "Отправить",
    "Send diagnostic data": "Отправлять данные диагностики",
    "Send feedback": "Отправить отзыв",
    "Send report": "Create report",
    "Settings": "Настройки",
    "Special Offer": "Специальное предложение",
    "Subscription terms": "Условия подписки",
    "Tap Adapter not present or disabled.": "Адаптер TAP отсутствует или отключен.",
    "Tell us what you think. Your feedback helps us improve.": "Поделитесь с нами своим мнением. Ваши отзывы помогают нам развиваться.",
    "Terms and conditions": "Коммерческие условия",
    "Test Pro for free": "Попробуйте версию Pro бесплатно",
    "The amount of data that you consume. We do this to help calculate the costs of providing our VPN infrastructure": "Количество потребляемых вами данных. Это нужно, чтобы рассчитать расходы на предоставление нашей инфраструктуры VPN.",
    "The app's color theme will use the operating system setting unless you choose a different theme.": "К цветовой теме приложения будут применены настройки операционной системы, если вы не выберете другую тему.",
    "The app's color theme will use the operating system setting.": "К цветовой теме приложения будут применены настройки операционной системы.",
    "The connection is not private": "Соединение не является защищенным",
    "The connection is private": "Это защищеннoe соединение",
    "The selected location is invalid. Please check the network connection.": "Выбрано неверное местоположение. Проверьте сетевое соединение.",
    "Three-Month free trial, then only <span>{0}</span> monthly": "Три месяца бесплатно, затем всего <span>{0}</span> в месяц",
    "To encrypt your internet connection, Phantom VPN uses the neagent system service. It needs your permission to access the Keychain.": "Для шифрования подключения к Интернету Phantom VPN использует системную службу neagent. Для настройки нам необходимо ваше разрешение.",
    "To try the best experience we will give you unlimited data volume for 30 days for free.": "Ощутите максимальные возможности программы с неограниченным объемом данных бесплатно в течение 30 дней.",
    "Traffic limit reached": "Превышено ограничение трафика",
    "Trial": "Пробная версия",
    "Unknown": "Неизвестно",
    "Unknown error.": "Неизвестная ошибка.",
    "Unlimited": "Неограниченно",
    "Unlimited traffic": "Неограниченный объем трафика",
    "Unlimited traffic on: Windows, macOS, iOS, Android, Google Chrome": "Неограниченный объем трафика в ОС Windows, macOS, iOS, Android, Google Chrome",
    "Use fastest protocol when available": "Выбор быстрого протокола (при наличии)",
    "Use system settings": "Использовать настройки системы",
    "Verification code": "Код проверки",
    "Virtual location set to <a> {0} </a>": "Виртуальное местоположение: <a> {0} </a>",
    "Virtual location: {0}": "Виртуальное местоположение: {0}",
    "We sent a verification code to your phone number": "Код проверки отправлен на ваш телефонный номер.",
    "We want to be fully transparent about the data you agree to share with us. To provide you a smooth VPN experience we need a minimum set of data:": "Вы должны точно знать, какие данные вы соглашаетесь предоставлять нам. Для обеспечения удобства работы с VPN нам нужен минимальный набор данных.",
    "Whether you are a free or a paid user. It's important for our communications to be able to differentiate the two": "Являетесь ли вы пользователем бесплатной или платной версии. Это важно для нашей системы связи.",
    "Yearly:": "На год:",
    "You will be disconnected in {0} seconds.": "Вы будете отключены через {0} с.",
    "Your connection is secure": "Соединение безопасно",
    "Your connection is secure.": "Соединение безопасно",
    "Your connection is unsecure": "Соединение небезопасно",
    "Your internet connection was protected from prying eyes. Is this worth 5 stars?": "Ваше интернет-соединение было защищено от несанкционированного доступа. Заслуживает ли это 5 звезд?",
    "Your license will expire soon.<br/>Remaining days: {0}": "Срок действия вашей лицензии истекает.<br/>Осталось дней: {0}",
    "Your password must contain at least 8 characters, one digit, and one uppercase letter.": "Пароль должен содержать не менее 8 символов, одну цифру и одну заглавную букву.",
    "default": "по умолч.",
    "https://www.research.net/r/HLV5Q2H?": "https://www.research.net/r/HLV5Q2H?",
    "https://www.research.net/r/J5H53HC?": "https://www.research.net/r/J5H53HC?",
    "or get 500 MB if you <a> register </a>": "или <a> зарегистрируйтесь </a> и получите 500 МБ",
    "{0} out of {1} daily secured traffic": "{0} из {1} безопасного трафика (в день)",
    "{0} out of {1} monthly secured traffic": "{0} из {1} безопасного трафика (в месяц)",
    "{0} out of {1} weekly secured traffic": "{0} из {1} безопасного трафика (в неделю)",
    "{0} secured traffic": "Безопасный трафик: {0}",
    "%": "%",
    "{0} used": "Использовано: {0}"
  });
  /* jshint +W100 */
}]);

},{}],68:[function(require,module,exports){
"use strict";

angular.module('LauncherApp').run(['gettextCatalog', function (gettextCatalog) {
  /* jshint -W100 */
  gettextCatalog.setStrings('tr_TR', {
    "0 bytes": "0 bayt",
    "<span class=\"privacyDescription__text__bold\">We don't log your online activity or any information that can link you to any action</span>, such as downloading a file, or visiting a particular website.": "<span class=\"privacyDescription__text__bold\">Çevrimiçi etkinlik veya karşıdan dosya yüklemek ya da belirli bir siteyi ziyaret etmek gibi</span>, sizi bir eylemle ilişkilendirecek hiçbir bilgiyi günlüğe kaydetmeyiz.",
    "A new driver has been installed, please restart your computer to complete the installation": "Bağlantınızı güvenli kılmadan önce bilgisayarınızı yeniden başlatın.",
    "About": "Hakkında",
    "Access": "Devam",
    "Account details": "Hesap Ayrıntıları",
    "Agree and continue": "Kabul et ve devam et",
    "All free slots are currently taken.": "No slots free currently.",
    "Already have an account?": "Zaten bir hesabınız var mı?",
    "Always Allow": "Her Zaman İzin Ver",
    "Auto-connect VPN": "VPN otomatik bağlantı",
    "Auto-connect VPN for Wi-Fi networks": "Wi-Fi ağları için VPN·otomatik·bağlantı",
    "Automatically secure untrusted Wi-Fi networks": "Güvensiz Wi-Fi ağlarını otomatik olarak güvenli hale getir",
    "Avira Phantom VPN Pro released!": "Avira Phantom VPN Pro artık burada!",
    "Back": "Geri",
    "Block all internet traffic if VPN connection drops": "VPN bağlantısı düşerse tüm internet trafiğini engelle",
    "Block intrusive ads": "İzinsiz reklamları engelle",
    "Block malicious sites and content": "Zararlı siteleri ve içeriği engelle",
    "Buy": "Satın al",
    "Buy unlimited traffic": "Sınırsız trafik satın al",
    "By proceeding, you are accepting the <a href=\"#\" ng-click=\"openEula()\">End User License Agreement (EULA)</a>, and the <a href=\"#\" ng-click=\"openTermsAndConditions()\">Terms and Conditions</a>. Avira is fulfilling its duties to provide information in accordance with Articles 13 and 14 of the General Data Protection Regulation (GDPR) with the contents of the Privacy Policy and access thereto. You can find our Privacy Policy here: <a href=\"#\" ng-click=\"openPrivacyAndPolicy()\">{{getPrivacyPolicyLink()}}</a>": "Devam ederek <a href=\"#\" ng-click=\"openEula()\">Son Kullanıcı Lisans Sözleşmesini (EULA)</a> ve <a href=\"#\" ng-click=\"openTermsAndConditions()\">Kayıt ve Koşulları</a> kabul ediyorsunuz. Avira, Genel Veri Koruma Yönetmeliği (GDPR), Madde 13 ve 14 uyarınca Gizlilik İlkelerinin içeriği ve bu ilkelere nasıl erişileceği hakkında bilgi verme yükümlülüklerini yerine getirmektedir. Gizlilik İlkemizi burada bulabilirsiniz: <a href=\"#\" ng-click=\"openPrivacyAndPolicy()\">{{getPrivacyPolicyLink()}}</a>",
    "Cancel": "İptal",
    "Cannot resolve host address.": "Ana bilgisayar adresi çözülemiyor.",
    "Change later in Settings": "Daha sonra Ayarlarda değiştir",
    "Check your inbox to confirm it's you. Make sure to look into your spam and junk folders as well.": "Sizin olduğunu onaylamak için gelen kutunuza bakın. İstenmeyen posta ve önemsiz posta kutularınıza da mutlaka bakın.",
    "Choose your color theme": "Renk teması seçin",
    "Code": "Kod",
    "Collect diagnostic data": "Gönder",
    "Connect": "Bağlan",
    "Connecting": "Bağlanıyor",
    "Connecting...": "Bağlanıyor…",
    "Connection to server lost.": "Sunucu ile bağlantı kesildi.",
    "Continue": "Devam",
    "Dark": "Koyu",
    "Dark theme": "Karanlık tema",
    "Disconnect": "Bağlantıyı kes",
    "Disconnecting": "Bağlantı kesiliyor",
    "Disconnecting...": "Bağlantı kesiliyor...",
    "Display settings": "Görüntü ayarları",
    "Don't have an account yet?": "Hesabınız mı yok?",
    "Don't send": "Gönderme",
    "Don't show again": "Tekrar gösterme",
    "Don't show this again": "Bunu tekrar gösterme",
    "Don't want to wait? Upgrade now": "Why wait? Upgrade now",
    "Done": "Bitti",
    "Email address": "E-posta adresi",
    "Email confirmation": "E-posta onayı",
    "Email sent": "Gönderildi",
    "Enjoy your unlimited data volume.<br/>Remaining days: {0}": "Sınırsız veri hacminin keyfini çıkartın.<br/>Kalan gün: {0}",
    "Enter a valid email address first": "Geçerli bir e-posta adresi gir",
    "Enter a valid password": "Geçerli bir parola girin",
    "Enter a valid verification code.": "Geçerli bir doğrulama kodu girin.",
    "Enter your Mac password and choose <span class=\"privacyDescription__text__bold\">Always allow</span> in the next window to continue.": "Devam etmek için Mac parolanızı girin ve sonraki pencerede <span class=\"privacyDescription__text__bold\">Her Zaman İzin Ver</span> seçeneğini seçin.",
    "Establishing connection...": "Bağlantı kuruluyor...",
    "Exit": "Çıkış",
    "Failed to connect to the service.": "Hizmete bağlanma başarısız.",
    "Failed to connect using UDP protocol. Retrying with TCP protocol.": "UDP protokolü kullanılarak bağlanılamadı. TCP protokolü ile yeniden deneniyor.",
    "Failed to establish the VPN connection. Please try again.": "Bağlanamıyor. Lütfen tekrar deneyin.",
    "Failed to purchase. Please try again later.": "Satın alma başarısız Lütfen sonra tekrar deneyin.",
    "Fatal error.": "Önemli hata.",
    "Fill in this field.": "Bu alanı doldurun.",
    "For security reasons your account has been blocked. Please access Avira Connect to unblock it.": "Hesabınız kilitlendi. Avira Connect'e erişerek kilidi kaldırabilirsiniz.",
    "Forgot your password?": "Şifrenizi mi unuttunuz?",
    "Get 3 Months Free": "3 ay Pro sürümünü edinin",
    "Get Pro": "Pro sürümünü edinin",
    "Get all the protection you need.": "Gerekli tüm korumayı edinin.",
    "Get for this occasion your three-month free trial. Thank you very much for helping us in beta stage by using and testing our product.": "Üç aylık ücretsiz özel teklifimizi edinin. Ürünü denediğiniz ve geliştirmeye yardım ettiğiniz için teşekkür ederiz.",
    "Get started": "Başla",
    "Get support": "Destek alın",
    "Get unlimited data volume now.": "Şimdi sınırsız erişim edinin.",
    "Help Avira improve its products and services by automatically sending daily anonymous diagnostic and usage data.": "Tanı ve kullanım verilerinizi her gün otomatik olarak Avira'ya göndererek ürün ve hizmetlerini geliştirmesine yardım edin.",
    "Help us improve": "Geliştirmeye yardım edin",
    "I entered a wrong email": "Yanlış e-posta adresi girdim",
    "IPSec traffic is blocked. Please contact your network administrator.": "IPSec trafiği engellendi. Lütfen ağ yöneticisi ile iletişim kurun.",
    "In case of technical issues you can collect diagnostic data and send a report to the developers.": "Having technical issues? We're here for you. Send us a report and we'll do the rest.",
    "Invalid credentials": "Geçersiz bilgiler",
    "Launch at system start": "Sistem başlangıcında başlat",
    "Length of subscription: 1 month/1 year<br><br> \n                    Price of subscription: 4,95€ per month (Mac only) – 7,95€ per month (all devices) – 59,95€ per year (all devices).<br><br>\n                    \n                    Payment will be charged to iTunes Account at confirmation of purchase. Subscription automatically renews unless auto-renew is turned off at least 24-hours before the end of the current period. Account will be charged for renewal within 24-hours prior to the end of the current period, and identify the cost of the renewal. Subscriptions may be managed by the user and auto-renewal may be turned off by going to the user's Account Settings after purchase. Any unused portion of a free trial period, if offered, will be forfeited when the user purchases a subscription to that publication, where applicable.": "Abonelik süresi: 1 ay/1 yıl - Abonelik fiyatı: 4,95€ aylık (sadece Mac) - 7,95€ aylık (tüm aygıtlar) - 59,95€ yıllık (tüm aygıtlar). Satın alma onayı sırasında iTunes Hesabı ücret ödemesi için borçlandırılır. Otomatik yenileme özelliği güncel dönem sona ermeden en az 24 saat önce kapatılmazsa abonelik otomatik olarak yenilenir. Güncel dönem sona ermeden önceki 24 saat içinde, hesap yenileme için borçlandırılır ve yenilemenin fiyatı belirtilir. Kullanıcı abonelikleri yönetilebilir ve satın alma sonrasında otomatik yenileme kullanıcı tarafından Hesap Ayarlarına giderek kapatabilir. Kullanıcı bu yayını satın aldığında, eğer teklif edilmişse, ücretsiz deneme süresinin kullanılmayan bölümünden, geçerli olması durumunda vaz geçer.",
    "Light": "Açık",
    "Light theme": "Aydınlık tema",
    "Log in": "Giriş yap",
    "Log out": "Oturumu kapat",
    "Logging in...": "Giriş yapılıyor...",
    "Monthly:": "Aylık:",
    "My Account": "Hesabım",
    "New to Avira?": "Avira'da yeni misiniz?",
    "No Wi-Fi network saved yet for automatic VPN connection. Once saved, they will automatically appear here and can be configured.": "Otomatik VPN bağlantısı için henüz kaydedilmiş Wi-Fi ağı yok. Kaydedildikten sonra burada otomatik olarak görünür ve yapılandırılabilir.",
    "No network available.": "Kullanılabilir Ağ yok.",
    "No purchase found. Nothing to restore.": "Hiç satın alma yok. Hiç geri yükleme yok.",
    "Not Now": "Şimdi değil",
    "Not now": "Şimdi değil",
    "Okay": "Tamam",
    "On all my devices": "Tüm aygıtlarımda",
    "On my Mac": "Mac'imde",
    "On my PC": "PC'imde",
    "Oops. Sorry, there was a error in the authentication process. Try again later or contact Support.": "Eyvah! Üzgünüz, kimlik doğrulama işleminde bir hata oldu. Sonra tekrar deneyin veya Destek birimine ulaşın.",
    "Oops. Sorry, there was a error in the registration process. Try again later or contact Support.": "Eyvah! Üzgünüz, kayıt işleminde bir hata oldu. Sonra tekrar deneyin veya Destek birimine ulaşın.",
    "Oops. Sorry, this email address is already registered with another account.": "Eyvah! Üzgünüz, bu adres zaten başka bir hesaba kayıtlı.",
    "Password": "Parola",
    "Please register if you want to purchase Phantom VPN Pro on all your devices": "Tüm aygıtlarınız için Phantom VPN Pro satın almak üzere kaydolun.",
    "Privacy is important to us": "Gizlilik bizim için önemlidir",
    "Privacy policy": "Gizlilik ilkesi",
    "Purchasing...": "Satın alıyor...",
    "Quit": "Çık",
    "Rate 5 stars": "5 yıldız verin",
    "Rate Avira Phantom VPN": "Avira Phantom VPN'i derecelendir",
    "Register": "Kaydol",
    "Register for an Avira account<br>and get your 30 day free trial": "Bir Avira hesabına kaydolun<br>ve 30 gün ücretsiz deneme edinin",
    "Registering...": "Kaydoluyor...",
    "Renew now": "Şimdi yenile",
    "Resend email": "Tekrar gönder",
    "Restore purchase": "Satın alınmış ürünü geri yükle",
    "Restoring...": "Geri yüklüyor...",
    "Secure my connection": "Bağlantımı güvenli yap",
    "Securing your connection": "Bağlantınızı güvenli kılma",
    "Select virtual location": "Sanal konum seçin",
    "Send": "Gönder",
    "Send diagnostic data": "Tanı verilerini gönder",
    "Send feedback": "Geribildirim gönder",
    "Send report": "Create report",
    "Settings": "Ayarlar",
    "Special Offer": "Özel teklif",
    "Subscription terms": "Abonelik koşulları",
    "Tap Adapter not present or disabled.": "TAP Adaptörü yok veya devre dışı.",
    "Tell us what you think. Your feedback helps us improve.": "Düşüncelerinizi bizimle paylaşın. Geribildiriminiz gelişime yardım ediyor.",
    "Terms and conditions": "Kayıt ve Koşullar",
    "Test Pro for free": "Pro'yu ücretsiz deneyin",
    "The amount of data that you consume. We do this to help calculate the costs of providing our VPN infrastructure": "Tükettiğiniz veri miktarı. Bunu VPN altyapısı sağlamanın maliyetini hesaplamak için yapıyoruz.",
    "The app's color theme will use the operating system setting unless you choose a different theme.": "Farklı bir tema seçmezseniz, uygulamanın renk teması işletim sistemi ayarını kullanacak.",
    "The app's color theme will use the operating system setting.": "Uygulamanın renk teması işletim sistemi ayarını kullanacak.",
    "The connection is not private": "Bağlantı özel değil",
    "The connection is private": "Bağlantı özel",
    "The selected location is invalid. Please check the network connection.": "Seçilen konum geçersiz. Lütfen ağ bağlantınızı kontrol edin.",
    "Three-Month free trial, then only <span>{0}</span> monthly": "Üç aylık ücretsiz deneme, sonra ayda sadece <span>{0}</span>",
    "To encrypt your internet connection, Phantom VPN uses the neagent system service. It needs your permission to access the Keychain.": "Phantom VPN internet bağlantınızı şifrelemek için neagent sistem hizmetini kullanır. Bunu kurmak için bize izin vermelisiniz.",
    "To try the best experience we will give you unlimited data volume for 30 days for free.": "En iyi deneyimi yaşamanız için size 30 gün sınırsız veri hacmini ücretsiz sunacağız.",
    "Traffic limit reached": "Trafik sınırına erişildi",
    "Trial": "Sınama sürümü",
    "Unknown": "Bilinmiyor",
    "Unknown error.": "Bilinmeyen hata.",
    "Unlimited": "Sınırsız",
    "Unlimited traffic": "Sınırsız trafik",
    "Unlimited traffic on: Windows, macOS, iOS, Android, Google Chrome": "Sınırsız trafik: Windows, macOS, iOS, Android, Google Chrome",
    "Use fastest protocol when available": "Kullanılabilir durumdaysa en hızlı protokolü kullan",
    "Use system settings": "Sistem ayarını kullan",
    "Verification code": "Doğrulama kodu",
    "Virtual location set to <a> {0} </a>": "Sanal konum yeri: <a> {0} </a>",
    "Virtual location: {0}": "Sanal konum yeri: {0}",
    "We sent a verification code to your phone number": "Doğrulama kodunu telefon numaranıza gönderdik.",
    "We want to be fully transparent about the data you agree to share with us. To provide you a smooth VPN experience we need a minimum set of data:": "Bizimle paylaşmayı kabul ettiğiniz veriler konusunda tam olarak şeffaf olmak istiyoruz. Sorunsuz bir VPN deneyimi yaşatmak için sizden asgari bazı veriler almamız gerekli:",
    "Whether you are a free or a paid user. It's important for our communications to be able to differentiate the two": "Ücretsiz mi ücretli mi kullanıcı olduğunuz. Bu ikisini birbirinden ayırt edebilmek iletişim açısından bizim için önemlidir.",
    "Yearly:": "Yıllık:",
    "You will be disconnected in {0} seconds.": "Bağlantınız {0} saniye sonra kesilecek.",
    "Your connection is secure": "Bağlantınız güvenli",
    "Your connection is secure.": "Bağlantınız güvenli",
    "Your connection is unsecure": "Bağlantınız güvensiz",
    "Your internet connection was protected from prying eyes. Is this worth 5 stars?": "Internet bağlantınız gözetleyenlere karşı korumalı. Bunun değeri 5 yıldız eder mi?",
    "Your license will expire soon.<br/>Remaining days: {0}": "Lisansınızın süresi yakında dolacak.<br/>Kalan gün: {0}",
    "Your password must contain at least 8 characters, one digit, and one uppercase letter.": "Parolanız bir rakam ve bir büyük harf içeren en az 8 karakter içermelidir.",
    "default": "varsayılan",
    "https://www.research.net/r/HLV5Q2H?": "https://www.research.net/r/HLV5Q2H?",
    "https://www.research.net/r/J5H53HC?": "https://www.research.net/r/J5H53HC?",
    "or get 500 MB if you <a> register </a>": "veya <a> kaydolun </a> ve 500 MB edinin",
    "{0} out of {1} daily secured traffic": "{0} / {1} günlük güvenli trafik",
    "{0} out of {1} monthly secured traffic": "%@ / %@ aylık güvenli trafik",
    "{0} out of {1} weekly secured traffic": "{0} / {1} haftalık güvenli trafik",
    "{0} secured traffic": "{0} güvenli trafik",
    "%": "%",
    "{0} used": "{0} kullanımda"
  });
  /* jshint +W100 */
}]);

},{}],69:[function(require,module,exports){
"use strict";

angular.module('LauncherApp').run(['gettextCatalog', function (gettextCatalog) {
  /* jshint -W100 */
  gettextCatalog.setStrings('zh_CN', {
    "0 bytes": "0 字节",
    "<span class=\"privacyDescription__text__bold\">We don't log your online activity or any information that can link you to any action</span>, such as downloading a file, or visiting a particular website.": "<span class=\"privacyDescription__text__bold\">我们不会记录您的在线活动，也不会记录可以将您和任何操作相关联的信息</span>，例如下载某个文件或者访问特定网站。",
    "A new driver has been installed, please restart your computer to complete the installation": "在保护您的连接前，请重新启动您的计算机。",
    "About": "关于",
    "Access": "继续",
    "Account details": "帐户详细信息",
    "Agree and continue": "保存并继续",
    "All free slots are currently taken.": "No slots free currently.",
    "Already have an account?": "已有帐户？?",
    "Always Allow": "始终允许",
    "Auto-connect VPN": "自动连接 VPN",
    "Auto-connect VPN for Wi-Fi networks": "针对 WiFi 网络自动连接 VPN",
    "Automatically secure untrusted Wi-Fi networks": "自动保护未受信任的 Wi-Fi 网络",
    "Avira Phantom VPN Pro released!": "Avira Phantom VPN Pro 已正式发布！!",
    "Back": "后退",
    "Block all internet traffic if VPN connection drops": "当 VPN 连接断开时切断所有 Internet 流量",
    "Block intrusive ads": "阻止入侵广告",
    "Block malicious sites and content": "阻止恶意网站和内容",
    "Buy": "购买",
    "Buy unlimited traffic": "购买无限制流量",
    "By proceeding, you are accepting the <a href=\"#\" ng-click=\"openEula()\">End User License Agreement (EULA)</a>, and the <a href=\"#\" ng-click=\"openTermsAndConditions()\">Terms and Conditions</a>. Avira is fulfilling its duties to provide information in accordance with Articles 13 and 14 of the General Data Protection Regulation (GDPR) with the contents of the Privacy Policy and access thereto. You can find our Privacy Policy here: <a href=\"#\" ng-click=\"openPrivacyAndPolicy()\">{{getPrivacyPolicyLink()}}</a>": "继续即表示您接受<a href=\"#\" ng-click=\"openEula()\">最终用户许可协议 (EULA)</a>、<a href=\"#\" ng-click=\"openTermsAndConditions()\">条款和条件</a>。Avira 遵从通用数据保护条例 (GDPR) 的第 13 和 14 款以及隐私政策的内容和相关访问权限履行提供信息的义务。您可在此找到我们的隐私政策：<a href=\"#\" ng-click=\"openPrivacyAndPolicy()\">{{getPrivacyPolicyLink()}}</a>",
    "Cancel": "取消",
    "Cannot resolve host address.": "无法解析主机地址。",
    "Change later in Settings": "稍后更改设置",
    "Check your inbox to confirm it's you. Make sure to look into your spam and junk folders as well.": "请查看您的收件箱确认您的身份。请确保查看垃圾邮件和垃圾文件夹.",
    "Choose your color theme": "选择您的颜色主题",
    "Code": "代码",
    "Collect diagnostic data": "发送",
    "Connect": "连接",
    "Connecting": "正在连接",
    "Connecting...": "正在连接…",
    "Connection to server lost.": "失去与服务器的连接。",
    "Continue": "继续",
    "Dark": "暗黑",
    "Dark theme": "深色主题",
    "Disconnect": "断开连接",
    "Disconnecting": "正在断开连接",
    "Disconnecting...": "正在断开连接...",
    "Display settings": "显示设置",
    "Don't have an account yet?": "没有帐户？",
    "Don't send": "不发送",
    "Don't show again": "不再显示",
    "Don't show this again": "不再显示",
    "Don't want to wait? Upgrade now": "Why wait? Upgrade now",
    "Done": "完成",
    "Email address": "电子邮件地址",
    "Email confirmation": "通过电子邮件确认",
    "Email sent": "电子邮件已发送",
    "Enjoy your unlimited data volume.<br/>Remaining days: {0}": "尽享无限制的数据流量。<br/>剩余天数：{0}",
    "Enter a valid email address first": "请输入有效的电子邮件地址",
    "Enter a valid password": "输入有效的密码",
    "Enter a valid verification code.": "请输入有效的验证代码.",
    "Enter your Mac password and choose <span class=\"privacyDescription__text__bold\">Always allow</span> in the next window to continue.": "输入您的 Mac 密码并在下一个窗口选择<span class=\"privacyDescription__text__bold\">始终允许</span>以继续。",
    "Establishing connection...": "正在建立连接...",
    "Exit": "退出",
    "Failed to connect to the service.": "无法连接至服务。",
    "Failed to connect using UDP protocol. Retrying with TCP protocol.": "无法使用 UDP 协议进行连接。正在使用 TCP 协议进行重试.",
    "Failed to establish the VPN connection. Please try again.": "无法连接。请重新尝试。",
    "Failed to purchase. Please try again later.": "购买失败。请稍后重试。",
    "Fatal error.": "出现致命错误.",
    "Fill in this field.": "填写此字段.",
    "For security reasons your account has been blocked. Please access Avira Connect to unblock it.": "您的帐户已被锁定。您只需访问 Avira Connect 即可将其解锁。",
    "Forgot your password?": "忘记密码？",
    "Get 3 Months Free": "获取 3 个月专业版试用",
    "Get Pro": "获取专业版",
    "Get all the protection you need.": "获取所有需要的防护。",
    "Get for this occasion your three-month free trial. Thank you very much for helping us in beta stage by using and testing our product.": "获取 3 个月免费特别优惠。感谢您试用我们的产品，并帮助我们不断完善此产品。",
    "Get started": "开始",
    "Get support": "获取技术支持",
    "Get unlimited data volume now.": "立即获得无限制访问权。",
    "Help Avira improve its products and services by automatically sending daily anonymous diagnostic and usage data.": "每日自动发送匿名的诊断和使用数据，以帮助 Avira 改进产品和服务。",
    "Help us improve": "帮助我们进行改进",
    "I entered a wrong email": "我输入的电子邮件地址错误",
    "IPSec traffic is blocked. Please contact your network administrator.": "IPSec 流量已被阻止。请与网络管理员取得联系。",
    "In case of technical issues you can collect diagnostic data and send a report to the developers.": "Having technical issues? We're here for you. Send us a report and we'll do the rest.",
    "Invalid credentials": "凭据无效",
    "Launch at system start": "系统启动时启动",
    "Length of subscription: 1 month/1 year<br><br> \n                    Price of subscription: 4,95€ per month (Mac only) – 7,95€ per month (all devices) – 59,95€ per year (all devices).<br><br>\n                    \n                    Payment will be charged to iTunes Account at confirmation of purchase. Subscription automatically renews unless auto-renew is turned off at least 24-hours before the end of the current period. Account will be charged for renewal within 24-hours prior to the end of the current period, and identify the cost of the renewal. Subscriptions may be managed by the user and auto-renewal may be turned off by going to the user's Account Settings after purchase. Any unused portion of a free trial period, if offered, will be forfeited when the user purchases a subscription to that publication, where applicable.": "订购时间：1 个月/1 年 - 订购价格：4.95€ /月（仅 iOS） - 7.95€ /月（所有设备） - 59.95€ /年（所有设备）。确认购买后，费用将通过 iTunes 帐户收取。如果您在当前有效期结束前 24 小时内不关闭自动续订，订购将自动续订。如您在当前有效期结束前 24 小时内续订成功，我们将对您的帐户收取费用，并确认续订费用。用户可对订购进行管理，自动续订可在购买后通过用户的帐户设置关闭。免费试用期间的未使用部分（如提供），在适用的情况下，将在用户订购该产品后失效.",
    "Light": "浅色",
    "Light theme": "浅色主题",
    "Log in": "登录",
    "Log out": "注销",
    "Logging in...": "正在登录…",
    "Monthly:": "每月：",
    "My Account": "我的帐户",
    "New to Avira?": "初次使用 Avira？",
    "No Wi-Fi network saved yet for automatic VPN connection. Once saved, they will automatically appear here and can be configured.": "尚未针对自动 VPN 连接保存任何 WiFi 网络。保存后的网络将自动显示在此处，可在此进行配置.",
    "No network available.": "无可用网络。",
    "No purchase found. Nothing to restore.": "未找到购买记录。无可还原项目。",
    "Not Now": "暂时不",
    "Not now": "暂时不",
    "Okay": "确定",
    "On all my devices": "我的所有设备",
    "On my Mac": "Mac 设备",
    "On my PC": "在我的 PC 上",
    "Oops. Sorry, there was a error in the authentication process. Try again later or contact Support.": "抱歉。抱歉，身份验证过程存在错误。请稍后重试或联系技术支持。",
    "Oops. Sorry, there was a error in the registration process. Try again later or contact Support.": "抱歉。抱歉，注册进程中存在错误。请稍后重试或联系技术支持。",
    "Oops. Sorry, this email address is already registered with another account.": "抱歉。抱歉，此电子邮件地址已注册到另一帐户.",
    "Password": "密码",
    "Please register if you want to purchase Phantom VPN Pro on all your devices": "请注册以便为您的所有设备购买 Phantom VPN Pro.",
    "Privacy is important to us": "隐私对我们非常重要",
    "Privacy policy": "隐私政策",
    "Purchasing...": "正在购买...",
    "Quit": "退出",
    "Rate 5 stars": "给出 5 星评价",
    "Rate Avira Phantom VPN": "评价 Avira Phantom VPN",
    "Register": "注册",
    "Register for an Avira account<br>and get your 30 day free trial": "注册 Avira 帐户<br>并获取 30 天免费试用版",
    "Registering...": "正在注册...",
    "Renew now": "现在续订",
    "Resend email": "重新发送电子邮件",
    "Restore purchase": "恢复购买",
    "Restoring...": "正在还原...",
    "Secure my connection": "保护我的连接",
    "Securing your connection": "确保您的连接安全性",
    "Select virtual location": "选择虚拟位置",
    "Send": "发送",
    "Send diagnostic data": "发送诊断数据",
    "Send feedback": "发送反馈",
    "Send report": "Create report",
    "Settings": "设置",
    "Special Offer": "特别优惠",
    "Subscription terms": "订购条款",
    "Tap Adapter not present or disabled.": "TAP 适配器不存在或已禁用。",
    "Tell us what you think. Your feedback helps us improve.": "告诉我们您的想法。 您的反馈将帮助我们改善用户体验。",
    "Terms and conditions": "条款和条件",
    "Test Pro for free": "免费测试专业版",
    "The amount of data that you consume. We do this to help calculate the costs of providing our VPN infrastructure": "您的数据消耗量。我们进行此操作的目的是协助计算提供 VPN 基础架构所需的成本.",
    "The app's color theme will use the operating system setting unless you choose a different theme.": "应用程序的颜色主题将使用操作系统设置，除非您选择了不同的主题。",
    "The app's color theme will use the operating system setting.": "应用程序的颜色主题将使用操作系统设置。",
    "The connection is not private": "此连接并非专用连接",
    "The connection is private": "此连接为专用连接",
    "The selected location is invalid. Please check the network connection.": "所选位置无效，请检查网络连接.",
    "Three-Month free trial, then only <span>{0}</span> monthly": "3 个月免费试用，此后仅需 <span>{0}</span>/月",
    "To encrypt your internet connection, Phantom VPN uses the neagent system service. It needs your permission to access the Keychain.": "Phantom VPN 需要使用 neagent 系统服务才能将您的 Internet 连接加密。我们需要您的授权才能对其进行设置。",
    "To try the best experience we will give you unlimited data volume for 30 days for free.": "为了让您获得最佳使用体验，我们将免费为您提供 30 天的无限数据流量.",
    "Traffic limit reached": "已达到流量上限",
    "Trial": "试用",
    "Unknown": "未知",
    "Unknown error.": "出现未知错误.",
    "Unlimited": "无限制",
    "Unlimited traffic": "无流量限制",
    "Unlimited traffic on: Windows, macOS, iOS, Android, Google Chrome": "以下系统可无限制使用流量：Windows、macOS、iOS、Android、Google Chrome",
    "Use fastest protocol when available": "使用最快的协议（如果可用）",
    "Use system settings": "使用系统设置",
    "Verification code": "验证代码",
    "Virtual location set to <a> {0} </a>": "虚拟位置位于: <a> {0} </a>",
    "Virtual location: {0}": "虚拟位置位于: {0}",
    "We sent a verification code to your phone number": "我们已向您的电话号码发送验证代码.",
    "We want to be fully transparent about the data you agree to share with us. To provide you a smooth VPN experience we need a minimum set of data:": "我们对于您同意与我们分享的数据采取全透明化处理方式。为了向您提供顺畅的 VPN 体验，我们至少需要以下数量的数据集：",
    "Whether you are a free or a paid user. It's important for our communications to be able to differentiate the two": "您是免费用户还是付费用户。区分这两者对于我们之间的通信非常重要。",
    "Yearly:": "每年：",
    "You will be disconnected in {0} seconds.": "您将在 {0} 秒后断开连接.",
    "Your connection is secure": "您的连接非常安全",
    "Your connection is secure.": "您的连接非常安全",
    "Your connection is unsecure": "您的连接不安全",
    "Your internet connection was protected from prying eyes. Is this worth 5 stars?": "我们保护您的 Internet 连接不受窥视。这值 5 星评价吗？",
    "Your license will expire soon.<br/>Remaining days: {0}": "您的许可证即将过期。<br/>剩余天数：{0}",
    "Your password must contain at least 8 characters, one digit, and one uppercase letter.": "您的密码必须包含至少 8 个字符、一个数字和一个大写字母.",
    "default": "默认",
    "https://www.research.net/r/HLV5Q2H?": "https://www.research.net/r/HLV5Q2H?",
    "https://www.research.net/r/J5H53HC?": "https://www.research.net/r/J5H53HC?",
    "or get 500 MB if you <a> register </a>": "或者<a>注册</a>获取 500 MB",
    "{0} out of {1} daily secured traffic": "{0} / {1} 每日流量受到保护",
    "{0} out of {1} monthly secured traffic": "{0} / {1} 每月流量受到保护",
    "{0} out of {1} weekly secured traffic": "{0} / {1} 每周流量受到保护",
    "{0} secured traffic": "{0} 流量受到保护",
    "%": "%",
    "{0} used": "{0} 已使用"
  });
  /* jshint +W100 */
}]);

},{}],70:[function(require,module,exports){
"use strict";

angular.module('LauncherApp').run(['gettextCatalog', function (gettextCatalog) {
  /* jshint -W100 */
  gettextCatalog.setStrings('zh_TW', {
    "0 bytes": "0 位元組",
    "<span class=\"privacyDescription__text__bold\">We don't log your online activity or any information that can link you to any action</span>, such as downloading a file, or visiting a particular website.": "<span class=\"privacyDescription__text__bold\">我們不會記錄您的線上活動或可以將您連結到任何動作的資訊</span>，例如下載檔案，或造訪特定網站。",
    "A new driver has been installed, please restart your computer to complete the installation": "在保護您的連線前，請重新啟動您的電腦。",
    "About": "關於",
    "Access": "繼續",
    "Account details": "帳戶詳細資料",
    "Agree and continue": "同意並繼續",
    "All free slots are currently taken.": "No slots free currently.",
    "Already have an account?": "已經有帳戶？?",
    "Always Allow": "一律允許",
    "Auto-connect VPN": "自動連接 VPN",
    "Auto-connect VPN for Wi-Fi networks": "對於無線網路自動連接 VPN",
    "Automatically secure untrusted Wi-Fi networks": "自動保護不受信任的 Wi-Fi 網路",
    "Avira Phantom VPN Pro released!": "Avira Phantom VPN Pro 已推出！!",
    "Back": "上一步",
    "Block all internet traffic if VPN connection drops": "VPN 連線中斷時封鎖所有網際網路流量",
    "Block intrusive ads": "封鎖干擾的廣告",
    "Block malicious sites and content": "封鎖惡意網站和內容",
    "Buy": "購買",
    "Buy unlimited traffic": "購買無限制的流量",
    "By proceeding, you are accepting the <a href=\"#\" ng-click=\"openEula()\">End User License Agreement (EULA)</a>, and the <a href=\"#\" ng-click=\"openTermsAndConditions()\">Terms and Conditions</a>. Avira is fulfilling its duties to provide information in accordance with Articles 13 and 14 of the General Data Protection Regulation (GDPR) with the contents of the Privacy Policy and access thereto. You can find our Privacy Policy here: <a href=\"#\" ng-click=\"openPrivacyAndPolicy()\">{{getPrivacyPolicyLink()}}</a>": "您繼續進行表示您接受<a href=\"#\" ng-click=\"openEula()\">使用者授權合約 (EULA)</a>、<a href=\"#\" ng-click=\"openTermsAndConditions()\">條款與條件</a>。Avira 善盡職責依據一般資料保護法規 (GDPR) 第 13 條和第 14 條及本文的隱私權政策和內容存取提供資訊。您可在此找到我們的隱私權政策：<a href=\"#\" ng-click=\"openPrivacyAndPolicy()\">{{getPrivacyPolicyLink()}}</a>",
    "Cancel": "取消",
    "Cannot resolve host address.": "無法解析主機位址。",
    "Change later in Settings": "稍後在設定中變更",
    "Check your inbox to confirm it's you. Make sure to look into your spam and junk folders as well.": "檢查您的收件匣確認您的身分。務必查看垃圾郵件和垃圾郵件資料夾.",
    "Choose your color theme": "選擇您的顏色主題",
    "Code": "代碼",
    "Collect diagnostic data": "傳送",
    "Connect": "連線",
    "Connecting": "正在連線",
    "Connecting...": "正在連線…",
    "Connection to server lost.": "伺服器的連線中斷。",
    "Continue": "繼續",
    "Dark": "暗黑",
    "Dark theme": "深色主題",
    "Disconnect": "中斷連線",
    "Disconnecting": "正在中斷連線",
    "Disconnecting...": "正在中斷連線...",
    "Display settings": "顯示設定",
    "Don't have an account yet?": "無帳戶？",
    "Don't send": "請勿傳送",
    "Don't show again": "不要再顯示",
    "Don't show this again": "不要再顯示",
    "Don't want to wait? Upgrade now": "Why wait? Upgrade now",
    "Done": "完成",
    "Email address": "電子郵件地址",
    "Email confirmation": "電子郵件確認",
    "Email sent": "已傳送電子郵件",
    "Enjoy your unlimited data volume.<br/>Remaining days: {0}": "盡情使用無限制的資料量。<br/>剩餘天數：{0}",
    "Enter a valid email address first": "輸入有效的電子郵件地址",
    "Enter a valid password": "輸入有效的密碼",
    "Enter a valid verification code.": "輸入有效的驗證代碼.",
    "Enter your Mac password and choose <span class=\"privacyDescription__text__bold\">Always allow</span> in the next window to continue.": "請輸入您的 Mac 密碼並在下一個窗口選擇<span class=\"privacyDescription__text__bold\">一律允許</span>繼續進行。",
    "Establishing connection...": "正在建立連線...",
    "Exit": "結束",
    "Failed to connect to the service.": "無法連線至服務。",
    "Failed to connect using UDP protocol. Retrying with TCP protocol.": "無法使用 UDP 通訊協定連線。正在使用 TCP 通訊協定重試.",
    "Failed to establish the VPN connection. Please try again.": "無法連線。請重新嘗試。",
    "Failed to purchase. Please try again later.": "購買失敗。請稍後再試一次。",
    "Fatal error.": "嚴重錯誤.",
    "Fill in this field.": "填寫此欄位.",
    "For security reasons your account has been blocked. Please access Avira Connect to unblock it.": "已鎖定您的帳戶。您可以存取 Avira Connect 將它解除鎖定。",
    "Forgot your password?": "忘記密碼？",
    "Get 3 Months Free": "取得可用 3 個月的專業版",
    "Get Pro": "获取专业版",
    "Get all the protection you need.": "獲得所需的一切防護。",
    "Get for this occasion your three-month free trial. Thank you very much for helping us in beta stage by using and testing our product.": "獲得 3 個月免費特殊優惠。感謝您試用我們的產品，並協助我們改善產品。",
    "Get started": "開始使用",
    "Get support": "取得技術支援",
    "Get unlimited data volume now.": "即刻獲取無限制訪問權。",
    "Help Avira improve its products and services by automatically sending daily anonymous diagnostic and usage data.": "透過自動傳送每日匿名診斷和使用資料，即可協助 Avira 改善產品和服務。",
    "Help us improve": "協助我們改善",
    "I entered a wrong email": "我輸入錯誤的電子郵件地址",
    "IPSec traffic is blocked. Please contact your network administrator.": "IPSec 流量被封鎖。請聯絡網路管理員。",
    "In case of technical issues you can collect diagnostic data and send a report to the developers.": "Having technical issues? We're here for you. Send us a report and we'll do the rest.",
    "Invalid credentials": "憑證無效",
    "Launch at system start": "系統啟動時啟動",
    "Length of subscription: 1 month/1 year<br><br> \n                    Price of subscription: 4,95€ per month (Mac only) – 7,95€ per month (all devices) – 59,95€ per year (all devices).<br><br>\n                    \n                    Payment will be charged to iTunes Account at confirmation of purchase. Subscription automatically renews unless auto-renew is turned off at least 24-hours before the end of the current period. Account will be charged for renewal within 24-hours prior to the end of the current period, and identify the cost of the renewal. Subscriptions may be managed by the user and auto-renewal may be turned off by going to the user's Account Settings after purchase. Any unused portion of a free trial period, if offered, will be forfeited when the user purchases a subscription to that publication, where applicable.": "訂閱時間長度：1 個月/1 年 - 訂閱價格：每月 4.95€ (僅限 Mac) - 每月 7.95€ (全部裝置) - 每年 59.95€ (全部裝置)。確認購買將向 iTunes 帳戶收費。除非在目前期間結束前至少 24 小時關閉自動更新，否則訂閱將自動更新。在目前期間結束前 24 小時內，將向帳戶收取更新的費用，並指出更新的金額。使用者可管理訂閱，而且可在購買後進入使用者的帳戶設定關閉自動更新。使用者購買適用產品的訂閱時，免費試用期間的任何未使用部份將失效。",
    "Light": "淺色",
    "Light theme": "淺色主題",
    "Log in": "登入",
    "Log out": "登出",
    "Logging in...": "登入中…",
    "Monthly:": "每月：",
    "My Account": "我的帳戶",
    "New to Avira?": "初次使用 Avira？",
    "No Wi-Fi network saved yet for automatic VPN connection. Once saved, they will automatically appear here and can be configured.": "尚未對於自動 VPN 連線儲存任何無線網路。儲存後，這些無線網路將自動出現在此處，而且可加以設定.",
    "No network available.": "無網路可用。",
    "No purchase found. Nothing to restore.": "找不到購買。沒有可還原的項目。",
    "Not Now": "現在不要",
    "Not now": "現在不要",
    "Okay": "確定",
    "On all my devices": "所有我的裝置",
    "On my Mac": "在我的 Mac 上",
    "On my PC": "在我的電腦上",
    "Oops. Sorry, there was a error in the authentication process. Try again later or contact Support.": "抱歉。驗證過程發生錯誤，謹此致歉。請稍後再試一次或連絡技術支援。",
    "Oops. Sorry, there was a error in the registration process. Try again later or contact Support.": "抱歉。註冊過程發生錯誤，謹此致歉。請稍後再試一次或連絡技術支援。",
    "Oops. Sorry, this email address is already registered with another account.": "抱歉。此電子郵件地址已註冊到另一個帳戶.",
    "Password": "密碼",
    "Please register if you want to purchase Phantom VPN Pro on all your devices": "請註冊以便為您的所有裝置購買 Phantom VPN Pro.",
    "Privacy is important to us": "我們相當重視隱私權",
    "Privacy policy": "隱私權政策",
    "Purchasing...": "正在購買...",
    "Quit": "結束",
    "Rate 5 stars": "評比 5 顆星",
    "Rate Avira Phantom VPN": "評比 Avira Phantom VPN",
    "Register": "註冊",
    "Register for an Avira account<br>and get your 30 day free trial": "註冊 Avira 帳戶<br>即可獲得 30 天免費試用版",
    "Registering...": "註冊中...",
    "Renew now": "立即更新",
    "Resend email": "重新傳送電子郵件",
    "Restore purchase": "還原購買",
    "Restoring...": "正在還原...",
    "Secure my connection": "確保我的連線",
    "Securing your connection": "確保連接安全性",
    "Select virtual location": "選取虛擬位置",
    "Send": "傳送",
    "Send diagnostic data": "傳送診斷資料",
    "Send feedback": "傳送意見回應",
    "Send report": "Create report",
    "Settings": "設定",
    "Special Offer": "特殊優惠",
    "Subscription terms": "訂閱條款",
    "Tap Adapter not present or disabled.": "TAP 介面卡不存在或已停用。",
    "Tell us what you think. Your feedback helps us improve.": "告訴我們您的看法。您的意見有助於我們改善。",
    "Terms and conditions": "條款與條件",
    "Test Pro for free": "免費試用專業版",
    "The amount of data that you consume. We do this to help calculate the costs of providing our VPN infrastructure": "您使用的資料數量。我們這麼做是為了計算提供 VPN 架構的成本.",
    "The app's color theme will use the operating system setting unless you choose a different theme.": "App 顏色主題將使用操作系統設定，除非您選擇了不同主題。",
    "The app's color theme will use the operating system setting.": "App 顏色主題將使用操作系統設定。",
    "The connection is not private": "該連線不是個人的連線",
    "The connection is private": "該連線是個人的連線",
    "The selected location is invalid. Please check the network connection.": "選取的位置無效。請檢查網路連線.",
    "Three-Month free trial, then only <span>{0}</span> monthly": "免費試用 3 個月，此後每個月只要 <span>{0}</span>",
    "To encrypt your internet connection, Phantom VPN uses the neagent system service. It needs your permission to access the Keychain.": "若要將您的網際網路連線加密，Phantom VPN 將使用 neagent 系統服務。我們需要您授權安裝。",
    "To try the best experience we will give you unlimited data volume for 30 days for free.": "為了達到最佳的使用體驗，我們為您提供 30 天免費使用的無限制資料量.",
    "Traffic limit reached": "達到流量限制",
    "Trial": "試用",
    "Unknown": "不明",
    "Unknown error.": "不明的錯誤.",
    "Unlimited": "無限",
    "Unlimited traffic": "流量無限制",
    "Unlimited traffic on: Windows, macOS, iOS, Android, Google Chrome": "流量無限制：Windows、macOS、iOS、Android、Google Chrome",
    "Use fastest protocol when available": "盡可能使用最快速的通訊協定",
    "Use system settings": "使用系統設定",
    "Verification code": "驗證代碼",
    "Virtual location set to <a> {0} </a>": "虛擬位置是在: <a> {0} </a>",
    "Virtual location: {0}": "虛擬位置是在: {0}",
    "We sent a verification code to your phone number": "我們將驗證代碼傳送到您的電話號碼.",
    "We want to be fully transparent about the data you agree to share with us. To provide you a smooth VPN experience we need a minimum set of data:": "對於您同意與我們分享的資料，我們想要達到完全的透明度。為了提供流暢的 VPN 體驗，我們需要最低限度的資料：",
    "Whether you are a free or a paid user. It's important for our communications to be able to differentiate the two": "您是免費使用者還是付費使用者。能夠區別這兩者對於我們的通訊相當重要。",
    "Yearly:": "每年：",
    "You will be disconnected in {0} seconds.": "將在 {0} 秒內中斷連線.",
    "Your connection is secure": "您的連線安全",
    "Your connection is secure.": "您的連線安全",
    "Your connection is unsecure": "您的連線不安全",
    "Your internet connection was protected from prying eyes. Is this worth 5 stars?": "已保護網際網路連線不被窺視。是否可得 5 顆星？",
    "Your license will expire soon.<br/>Remaining days: {0}": "您的授權將到期。<br/>剩餘天數：{0}",
    "Your password must contain at least 8 characters, one digit, and one uppercase letter.": "您的密碼必須包含至少 8 個字元、一個數字和一個大寫字母.",
    "default": "預設值",
    "https://www.research.net/r/HLV5Q2H?": "https://www.research.net/r/HLV5Q2H?",
    "https://www.research.net/r/J5H53HC?": "https://www.research.net/r/J5H53HC?",
    "or get 500 MB if you <a> register </a>": "或者<a>註冊</a>即可獲得 500 MB",
    "{0} out of {1} daily secured traffic": "{0} / {1} 每日受保護的流量",
    "{0} out of {1} monthly secured traffic": "{0} / {1} 每月受保護的流量",
    "{0} out of {1} weekly secured traffic": "{0} / {1} 每週受保護的流量",
    "{0} secured traffic": "{0} 受保護的流量",
    "%": "%",
    "{0} used": "已使用 {0}"
  });
  /* jshint +W100 */
}]);

},{}],71:[function(require,module,exports){
"use strict";

var _defaultConfigurator = _interopRequireDefault(require("../../../../scripts/services/defaultConfigurator"));

function _interopRequireDefault(obj) { return obj && obj.__esModule ? obj : { "default": obj }; }

function _typeof(obj) { "@babel/helpers - typeof"; if (typeof Symbol === "function" && typeof Symbol.iterator === "symbol") { _typeof = function _typeof(obj) { return typeof obj; }; } else { _typeof = function _typeof(obj) { return obj && typeof Symbol === "function" && obj.constructor === Symbol && obj !== Symbol.prototype ? "symbol" : typeof obj; }; } return _typeof(obj); }

function _classCallCheck(instance, Constructor) { if (!(instance instanceof Constructor)) { throw new TypeError("Cannot call a class as a function"); } }

function _defineProperties(target, props) { for (var i = 0; i < props.length; i++) { var descriptor = props[i]; descriptor.enumerable = descriptor.enumerable || false; descriptor.configurable = true; if ("value" in descriptor) descriptor.writable = true; Object.defineProperty(target, descriptor.key, descriptor); } }

function _createClass(Constructor, protoProps, staticProps) { if (protoProps) _defineProperties(Constructor.prototype, protoProps); if (staticProps) _defineProperties(Constructor, staticProps); return Constructor; }

function _inherits(subClass, superClass) { if (typeof superClass !== "function" && superClass !== null) { throw new TypeError("Super expression must either be null or a function"); } subClass.prototype = Object.create(superClass && superClass.prototype, { constructor: { value: subClass, writable: true, configurable: true } }); if (superClass) _setPrototypeOf(subClass, superClass); }

function _setPrototypeOf(o, p) { _setPrototypeOf = Object.setPrototypeOf || function _setPrototypeOf(o, p) { o.__proto__ = p; return o; }; return _setPrototypeOf(o, p); }

function _createSuper(Derived) { var hasNativeReflectConstruct = _isNativeReflectConstruct(); return function _createSuperInternal() { var Super = _getPrototypeOf(Derived), result; if (hasNativeReflectConstruct) { var NewTarget = _getPrototypeOf(this).constructor; result = Reflect.construct(Super, arguments, NewTarget); } else { result = Super.apply(this, arguments); } return _possibleConstructorReturn(this, result); }; }

function _possibleConstructorReturn(self, call) { if (call && (_typeof(call) === "object" || typeof call === "function")) { return call; } return _assertThisInitialized(self); }

function _assertThisInitialized(self) { if (self === void 0) { throw new ReferenceError("this hasn't been initialised - super() hasn't been called"); } return self; }

function _isNativeReflectConstruct() { if (typeof Reflect === "undefined" || !Reflect.construct) return false; if (Reflect.construct.sham) return false; if (typeof Proxy === "function") return true; try { Date.prototype.toString.call(Reflect.construct(Date, [], function () {})); return true; } catch (e) { return false; } }

function _getPrototypeOf(o) { _getPrototypeOf = Object.setPrototypeOf ? Object.getPrototypeOf : function _getPrototypeOf(o) { return o.__proto__ || Object.getPrototypeOf(o); }; return _getPrototypeOf(o); }

module.exports = function (module) {
  module.factory('Configurator', ['gettextCatalog', function (gettextCatalog) {
    var Configurator = /*#__PURE__*/function (_DefaultConfigurator) {
      _inherits(Configurator, _DefaultConfigurator);

      var _super = _createSuper(Configurator);

      function Configurator() {
        _classCallCheck(this, Configurator);

        return _super.call(this);
      }

      _createClass(Configurator, [{
        key: "supportUrl",
        value: function supportUrl(lang) {
          return "https://www.avira.com/" + lang + "/vpn-support";
        }
      }, {
        key: "aboutUrl",
        value: function aboutUrl(lang) {
          return "https://www.avira.com/" + lang + "/vpn-legal-info";
        }
      }, {
        key: "getLabels",
        value: function getLabels() {
          return {
            ProductName: "Apollo",
            BrandName: "Apollo",
            SublogoTextPro: "",
            ContextMenuName: "Apollo.",
            ProBadge: ""
          };
        }
      }, {
        key: "getStrings",
        value: function getStrings() {
          return {
            getPro: gettextCatalog.getString("Get Pro"),
            disconnect: gettextCatalog.getString('Disconnect'),
            secureMyConnection: gettextCatalog.getString('Secure my connection'),
            cancel: gettextCatalog.getString('Cancel')
          };
        }
      }, {
        key: "useForcedLogin",
        get: function get() {
          return true;
        }
      }, {
        key: "feedbackUrlMac",
        get: function get() {
          return "https://www.avira.com";
        }
      }, {
        key: "feedbackUrlWin",
        get: function get() {
          return "https://www.avira.com";
        }
      }, {
        key: "showThemeSelection",
        get: function get() {
          return false;
        }
      }, {
        key: "useOsTheme",
        get: function get() {
          return false;
        }
      }, {
        key: "forceDarkTheme",
        get: function get() {
          return true;
        }
      }, {
        key: "showHelpButton",
        get: function get() {
          return false;
        }
      }, {
        key: "login_forgotPasswordUrl",
        get: function get() {
          return "https://subscriptions.zoho.com/portal/apollo2/forgetpassword";
        }
      }, {
        key: "login_registerUrl",
        get: function get() {
          return "https://www.joinapollo.cc/";
        }
      }, {
        key: "emailConfirmationNeeded",
        get: function get() {
          return false;
        }
      }, {
        key: "allowLogout",
        get: function get() {
          return false;
        }
      }, {
        key: "onlyProConnects",
        get: function get() {
          return true;
        }
      }, {
        key: "useOeAuth",
        get: function get() {
          return false;
        }
      }, {
        key: "openDashboardInUI",
        get: function get() {
          return true;
        }
      }, {
        key: "dashboardUrl",
        get: function get() {
          return "https://subscriptions.zoho.com/portal/apollo2";
        }
      }, {
        key: "renewalUrl",
        get: function get() {
          return "https://www.joinapollo.cc/";
        }
      }, {
        key: "showFeedbackOnDisconnect",
        get: function get() {
          return true;
        }
      }]);

      return Configurator;
    }(_defaultConfigurator["default"]);

    return new Configurator();
  }]);
};

},{"../../../../scripts/services/defaultConfigurator":45}],72:[function(require,module,exports){
"use strict";

var _defaultConfigurator = _interopRequireDefault(require("../../../../scripts/services/defaultConfigurator"));

function _interopRequireDefault(obj) { return obj && obj.__esModule ? obj : { "default": obj }; }

function _typeof(obj) { "@babel/helpers - typeof"; if (typeof Symbol === "function" && typeof Symbol.iterator === "symbol") { _typeof = function _typeof(obj) { return typeof obj; }; } else { _typeof = function _typeof(obj) { return obj && typeof Symbol === "function" && obj.constructor === Symbol && obj !== Symbol.prototype ? "symbol" : typeof obj; }; } return _typeof(obj); }

function _classCallCheck(instance, Constructor) { if (!(instance instanceof Constructor)) { throw new TypeError("Cannot call a class as a function"); } }

function _defineProperties(target, props) { for (var i = 0; i < props.length; i++) { var descriptor = props[i]; descriptor.enumerable = descriptor.enumerable || false; descriptor.configurable = true; if ("value" in descriptor) descriptor.writable = true; Object.defineProperty(target, descriptor.key, descriptor); } }

function _createClass(Constructor, protoProps, staticProps) { if (protoProps) _defineProperties(Constructor.prototype, protoProps); if (staticProps) _defineProperties(Constructor, staticProps); return Constructor; }

function _inherits(subClass, superClass) { if (typeof superClass !== "function" && superClass !== null) { throw new TypeError("Super expression must either be null or a function"); } subClass.prototype = Object.create(superClass && superClass.prototype, { constructor: { value: subClass, writable: true, configurable: true } }); if (superClass) _setPrototypeOf(subClass, superClass); }

function _setPrototypeOf(o, p) { _setPrototypeOf = Object.setPrototypeOf || function _setPrototypeOf(o, p) { o.__proto__ = p; return o; }; return _setPrototypeOf(o, p); }

function _createSuper(Derived) { var hasNativeReflectConstruct = _isNativeReflectConstruct(); return function _createSuperInternal() { var Super = _getPrototypeOf(Derived), result; if (hasNativeReflectConstruct) { var NewTarget = _getPrototypeOf(this).constructor; result = Reflect.construct(Super, arguments, NewTarget); } else { result = Super.apply(this, arguments); } return _possibleConstructorReturn(this, result); }; }

function _possibleConstructorReturn(self, call) { if (call && (_typeof(call) === "object" || typeof call === "function")) { return call; } return _assertThisInitialized(self); }

function _assertThisInitialized(self) { if (self === void 0) { throw new ReferenceError("this hasn't been initialised - super() hasn't been called"); } return self; }

function _isNativeReflectConstruct() { if (typeof Reflect === "undefined" || !Reflect.construct) return false; if (Reflect.construct.sham) return false; if (typeof Proxy === "function") return true; try { Date.prototype.toString.call(Reflect.construct(Date, [], function () {})); return true; } catch (e) { return false; } }

function _getPrototypeOf(o) { _getPrototypeOf = Object.setPrototypeOf ? Object.getPrototypeOf : function _getPrototypeOf(o) { return o.__proto__ || Object.getPrototypeOf(o); }; return _getPrototypeOf(o); }

module.exports = function (module) {
  module.factory('Configurator', ['gettextCatalog', function (gettextCatalog) {
    var Configurator = /*#__PURE__*/function (_DefaultConfigurator) {
      _inherits(Configurator, _DefaultConfigurator);

      var _super = _createSuper(Configurator);

      function Configurator() {
        _classCallCheck(this, Configurator);

        return _super.call(this);
      }

      _createClass(Configurator, [{
        key: "supportUrl",
        value: function supportUrl(lang) {
          return "https://www.invincibull.io/faq/";
        }
      }, {
        key: "aboutUrl",
        value: function aboutUrl(lang) {
          return "https://www.invincibull.io/";
        }
      }, {
        key: "getLabels",
        value: function getLabels() {
          return {
            ProductName: "Invincibull",
            BrandName: "Invincibull",
            SublogoTextPro: "Unlimited",
            ContextMenuName: "InvinciBull.",
            ProBadge: "UNLIMITED"
          };
        }
      }, {
        key: "getStrings",
        value: function getStrings() {
          return {
            getPro: gettextCatalog.getString("Get Unlimited"),
            disconnect: gettextCatalog.getString('DISCONNECT'),
            secureMyConnection: gettextCatalog.getString('SECURE MY CONNECTION'),
            cancel: gettextCatalog.getString('CANCEL')
          };
        }
      }, {
        key: "hideAccountText",
        get: function get() {
          return true;
        }
      }, {
        key: "hideAboutMenuEntry",
        get: function get() {
          return true;
        }
      }, {
        key: "regionsOnlyNearestFree",
        get: function get() {
          return true;
        }
      }, {
        key: "darkThemeAvailable",
        get: function get() {
          return false;
        }
      }, {
        key: "showAboutLink",
        get: function get() {
          return true;
        }
      }, {
        key: "showQuitLink",
        get: function get() {
          return true;
        }
      }, {
        key: "showLogoutLink",
        get: function get() {
          return true;
        }
      }, {
        key: "showThemeSelection",
        get: function get() {
          return false;
        }
      }, {
        key: "useOsTheme",
        get: function get() {
          return false;
        }
      }, {
        key: "feedbackUrlMac",
        get: function get() {
          return "https://www.invincibull.io/contact/";
        }
      }, {
        key: "feedbackUrlWin",
        get: function get() {
          return "https://www.invincibull.io/contact/";
        }
      }]);

      return Configurator;
    }(_defaultConfigurator["default"]);

    return new Configurator();
  }]);
};

},{"../../../../scripts/services/defaultConfigurator":45}],73:[function(require,module,exports){
"use strict";

var _defaultConfigurator = _interopRequireDefault(require("../../../../scripts/services/defaultConfigurator"));

function _interopRequireDefault(obj) { return obj && obj.__esModule ? obj : { "default": obj }; }

function _typeof(obj) { "@babel/helpers - typeof"; if (typeof Symbol === "function" && typeof Symbol.iterator === "symbol") { _typeof = function _typeof(obj) { return typeof obj; }; } else { _typeof = function _typeof(obj) { return obj && typeof Symbol === "function" && obj.constructor === Symbol && obj !== Symbol.prototype ? "symbol" : typeof obj; }; } return _typeof(obj); }

function _classCallCheck(instance, Constructor) { if (!(instance instanceof Constructor)) { throw new TypeError("Cannot call a class as a function"); } }

function _defineProperties(target, props) { for (var i = 0; i < props.length; i++) { var descriptor = props[i]; descriptor.enumerable = descriptor.enumerable || false; descriptor.configurable = true; if ("value" in descriptor) descriptor.writable = true; Object.defineProperty(target, descriptor.key, descriptor); } }

function _createClass(Constructor, protoProps, staticProps) { if (protoProps) _defineProperties(Constructor.prototype, protoProps); if (staticProps) _defineProperties(Constructor, staticProps); return Constructor; }

function _inherits(subClass, superClass) { if (typeof superClass !== "function" && superClass !== null) { throw new TypeError("Super expression must either be null or a function"); } subClass.prototype = Object.create(superClass && superClass.prototype, { constructor: { value: subClass, writable: true, configurable: true } }); if (superClass) _setPrototypeOf(subClass, superClass); }

function _setPrototypeOf(o, p) { _setPrototypeOf = Object.setPrototypeOf || function _setPrototypeOf(o, p) { o.__proto__ = p; return o; }; return _setPrototypeOf(o, p); }

function _createSuper(Derived) { var hasNativeReflectConstruct = _isNativeReflectConstruct(); return function _createSuperInternal() { var Super = _getPrototypeOf(Derived), result; if (hasNativeReflectConstruct) { var NewTarget = _getPrototypeOf(this).constructor; result = Reflect.construct(Super, arguments, NewTarget); } else { result = Super.apply(this, arguments); } return _possibleConstructorReturn(this, result); }; }

function _possibleConstructorReturn(self, call) { if (call && (_typeof(call) === "object" || typeof call === "function")) { return call; } return _assertThisInitialized(self); }

function _assertThisInitialized(self) { if (self === void 0) { throw new ReferenceError("this hasn't been initialised - super() hasn't been called"); } return self; }

function _isNativeReflectConstruct() { if (typeof Reflect === "undefined" || !Reflect.construct) return false; if (Reflect.construct.sham) return false; if (typeof Proxy === "function") return true; try { Date.prototype.toString.call(Reflect.construct(Date, [], function () {})); return true; } catch (e) { return false; } }

function _getPrototypeOf(o) { _getPrototypeOf = Object.setPrototypeOf ? Object.getPrototypeOf : function _getPrototypeOf(o) { return o.__proto__ || Object.getPrototypeOf(o); }; return _getPrototypeOf(o); }

module.exports = function (module) {
  module.factory('Configurator', ['gettextCatalog', function (gettextCatalog) {
    var Configurator = /*#__PURE__*/function (_DefaultConfigurator) {
      _inherits(Configurator, _DefaultConfigurator);

      var _super = _createSuper(Configurator);

      function Configurator() {
        _classCallCheck(this, Configurator);

        return _super.call(this);
      }

      _createClass(Configurator, [{
        key: "supportUrl",
        value: function supportUrl(lang) {
          return "https://www.avira.com/" + lang + "/vpn-support";
        }
      }, {
        key: "aboutUrl",
        value: function aboutUrl(lang) {
          return "https://www.avira.com/" + lang + "/vpn-legal-info";
        }
      }, {
        key: "getStrings",
        value: function getStrings() {
          return {
            getPro: gettextCatalog.getString("Get Pro"),
            disconnect: gettextCatalog.getString('Disconnect'),
            secureMyConnection: gettextCatalog.getString('Secure my connection'),
            cancel: gettextCatalog.getString('Cancel')
          };
        }
      }, {
        key: "feedbackUrlMac",
        get: function get() {
          return "https://www.avira.com";
        }
      }, {
        key: "feedbackUrlWin",
        get: function get() {
          return "https://www.avira.com";
        }
      }, {
        key: "showFeedbackOnDisconnect",
        get: function get() {
          return false;
        }
      }]);

      return Configurator;
    }(_defaultConfigurator["default"]);

    return new Configurator();
  }]);
};

},{"../../../../scripts/services/defaultConfigurator":45}]},{},[41,59,60,61,62,63,64,65,66,67,68,69,70,42,52,57,73,6,7,8])
//# sourceMappingURL=vpn-1.0.0.js.map
