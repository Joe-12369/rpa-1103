(function () {
    //基础方法
    IIRPA = {};
    IIRPA.api = {};
    IIRPA.dom = {};
    IIRPA.locale = {};
    IIRPA.table = {};
    IIRPA.ieVersion;
    IIRPA.traceEnabled = false;
    IIRPA.isZoomAppliedToRect = false;
    IIRPA.UNIQUE_KEY = "i-rpa";
    IIRPA.browserType = "";
    IIRPA.PropPattern = {
        EQUAL: 'equal',
        CONTAIN: 'contain',
        BEGIN: 'begin',
        END: 'end',
        REGULAR: 'regular',
        HAS: 'has',
        NOT_HAS: 'nothas',
        EMPTY: 'empty',
        NOT_EMPTY: 'notempty',
        EXCLUDE: 'exclude'
    };
    IIRPA.ActionName = {
        COUNT: 'count',
        INPUT: 'input',
        CLICK: 'click',
        POSITION: 'getposition',
        TEXT: 'text'
    };
    IIRPA.Location = {
        UPPERLEFT: "upperLeft",
        UPPERRIGHT: "upperRight",
        LOWERRIGHT: "lowerRight",
        LOWERLEFT: "lowerLeft",
        CENTER: "center",
        RANDOM: "random"
    }
    IIRPA.SpecialAttributes = {
        CLASS: "className",
        FOR: "htmlFor",
        TABINDEX: "tabIndex",
        READONLY: "readOnly",
        MAXLENGTH: "maxLength",
        COLSPAN: "colSpan",
        ROWSPAN: "rowSpan",
        USEMAP: "useMap",
        ISMAP: "isMap",
        NOHREF: "noHref",
        DECLARE: "declare",
        ACCEPTCHARSET: "acceptCharset",
        ENCTYPE: "encoding"
    };
    IIRPA.getBrowserType = function () {
        if (IIRPA.browserType != "") {
            return IIRPA.browserType;
        }
        var isOpera = (!!window.opr && !!opr.addons) || !!window.opera || navigator.userAgent.indexOf(' OPR/') >= 0;
        var isChrome = !!window.chrome && (!!window.chrome.webstore || !!window.chrome.runtime);
        var isIE = /*@cc_on!@*/false || !!document.documentMode;
        // Firefox 1.0+
        if (typeof InstallTrigger !== 'undefined') {
            //window.chrome = window.browser;
            IIRPA.browserType = "firefox"
        }
        else if (/^((?!chrome|android).)*safari/i.test(navigator.userAgent))
            // Safari 3.0+ "[object HTMLElementConstructor]"
            IIRPA.browserType = "safari"
        else if (isIE)
            // Internet Explorer 6-11
            IIRPA.browserType = "ie"
        else if (!isIE && !!window.StyleMedia)
            // Edge 20+
            IIRPA.browserType = "edge"
        else if (isChrome)
            // Chrome 1 - 79
            IIRPA.browserType = "chrome"
        else if (isChrome && (navigator.userAgent.indexOf("Edg") != -1))
            // Edge (based on chromium) detection
            IIRPA.browserType = "edge"
        else if ((isChrome || isOpera) && !!window.CSS)
            // Blink engine detection
            IIRPA.browserType = "blink"
        return IIRPA.browserType;
    }

    //IIRPA.buildOptionProp = function (name, value) {
    //    return { name: name, value: value || '' };
    //};
    IIRPA.buildProp = function (name, value, pattern, accurate) {
        if (accurate)
            return { name: name, value: value || '', pattern: pattern || IIRPA.PropPattern.EQUAL, accurate: "true" };
        else
            return { name: name, value: value || '', pattern: pattern || IIRPA.PropPattern.EQUAL, accurate: "false" };
    };
    //IIRPA.buildOptionProp = function (name, value, pattern) {
    //    return { name: name, value: value || '' };
    //};
    //optionProps暂时不实现 
    IIRPA.buildPath = function (tagName, props, optionProps) {
        //var p = []
        for (var i = 0; i < optionProps.length; i++) {
            for (var j = 0; j < props.length; j++) {
                if (optionProps[i].name === props[j].name) {
                    optionProps[i] = props[j]
                }
            }
        }
        return {
            name: tagName.toLowerCase(),
            props: optionProps,
            type: "web",
            accurate: "true"
        }
    };
    //异常code 
    IIRPA.ErrorCode = {
        SUCCESS: 0,
        NO_SUCH_ELEMENT: 1,
        INVALID_PARAM: 2,
        INVALID_CSS_SELECTOR: 3,
        NOT_EMPTY_PARAM: 4,
        Failure: 98,
        JAVASCRIPT_ERROR: 99
    };
    IIRPA.Error = function (code, message) {
        this.code = code;
        this.message = message || '';
    }
    IIRPA.checkEmpty = function (param, message) {
        if (!param) {
            throw new IIRPA.Error(IIRPA.ErrorCode.NOT_EMPTY_PARAM, message);
        }
    }
    IIRPA.ParseInt = function (value) {
        if (value) {
            return parseInt(value)
        }
        return 0;
    }
    IIRPA.Coordinate = function (x, y) {
        this.x = x || 0; this.y = y || 0;
    }
    IIRPA.isArray = function (val) {
        return IIRPA.typeOf(val) == 'array';
    };
    IIRPA.isNumber = function (number) {
        return number === +number;
    }
    IIRPA.isString = function (str) {
        return str === str + '';
    }
    IIRPA.trim = function (str) {
        if (!str)
            return "";
        if (!String.prototype.trim) {
            String.prototype.trim = function () {
                return this.replace(/^[\s\uFEFF\xA0]+|[\s\uFEFF\xA0]+$/g, '');
            };
        }
        return str.trim();
    }
    //获取页面缩放比例,非百分值
    IIRPA.zoomLevel = function () {
        try {
            return screen.deviceXDPI / screen.logicalXDPI;
        } catch (e) {
            return 1
        }
    }
    //IE7,8不支持此方法
    IIRPA.indexOfArray = function (array, searchElement, fromIndex) {
        if (!Array.prototype.indexOf) {
            Array.prototype.indexOf = function (searchElement, fromIndex) {
                var k;
                if (this == null) {
                    throw new TypeError('"this" is null or not defined');
                }
                var o = Object(this); var len = o.length >>> 0;
                // 4. If len is 0, return -1. 
                if (len === 0) { return -1; }
                var n = fromIndex | 0; if (n >= len) {
                    return -1;
                }
                k = Math.max(n >= 0 ? n : len - Math.abs(n), 0);
                while (k < len) {
                    if (k in o && o[k] === searchElement) {
                        return k;
                    }
                    k++;
                } return -1;
            };
        }
        return array.indexOf(searchElement, fromIndex);
    }
    //copy from google.typeof
    IIRPA.typeOf = function (value) {
        var s = typeof value;
        if (s == 'object') {
            if (value) {
                if (value instanceof Array) {
                    return 'array';
                } else if (value instanceof Object) {
                    return s;
                }
                var className = Object.prototype.toString.call(
                    /** 
                     *  @type {!Object} 
                     */
                    (value));
                if (className == '[object Window]') {
                    return 'object';
                }
                if ((className == '[object Array]' || typeof value.length == 'number' && typeof value.splice != 'undefined' && typeof value.propertyIsEnumerable != 'undefined' && !value.propertyIsEnumerable('splice'))) {
                    return 'array';
                }
                if ((className == '[object Function]' || typeof value.call != 'undefined' && typeof value.propertyIsEnumerable != 'undefined' && !value.propertyIsEnumerable('call'))) {
                    return 'function';
                }
            } else {
                return 'null';
            }
        } else if (s == 'function' && typeof value.call == 'undefined') {
            return 'object';
        }
        return s;
    };
    IIRPA.json_stringify = function (value, replacer, space) {
        var i;
        gap = "";
        indent = "";
        if (typeof space === "number") {
            for (i = 0; i < space; i += 1) {
                indent += " ";
            }
        } else if (typeof space === "string") {
            indent = space;
        }
        rep = replacer;
        if (replacer && typeof replacer !== "function" && (typeof replacer !== "object" || typeof replacer.length !== "number")) {
            throw new Error("JSON/JSON2.stringify");
        }
        return str("", { "": value });
    };
    IIRPA.escapeIdentifierIfNeeded = function (ident) {
        if (isCSSIdentifier(ident))
            return ident;
        var shouldEscapeFirst = /^(?:[0-9]|-[0-9-]?)/.test(ident);
        var lastIndex = ident.length - 1;
        return ident.replace(/./g, function (c, i) {
            return ((shouldEscapeFirst && i === 0) || !isCSSIdentChar(c)) ? escapeAsciiChar(c, i === lastIndex) : c;
        });
        /**      * @param {string} c      * @return {boolean}      */
        function isCSSIdentChar(c) {
            if (/[a-zA-Z0-9_-]/.test(c))
                return true;
            return c.charCodeAt(0) >= 0xA0;
        }
        function isCSSIdentifier(value) {
            return /^-?[a-zA-Z_][a-zA-Z0-9_-]*$/.test(value);
        }
        /**    * @param {string} c    * @param {boolean} isLast    * @return {string}    */
        function escapeAsciiChar(c, isLast) {
            return "\\" + toHexByte(c) + (isLast ? "" : " ");
        }
        /**    * @param {string} c    */
        function toHexByte(c) {
            var hexByte = c.charCodeAt(0).toString(16);
            if (hexByte.length === 1)
                hexByte = "0" + hexByte;
            return hexByte;
        }
    }
    IIRPA.regMatch = function (str, regExp) {
        try {
            str = IIRPA.trim(str);
            regExp = IIRPA.trim(regExp);
            if (!regExp) {
                return false
            }
            //存在某些特殊符号 - * . 等 需要吃掉异常
            var pattern = new RegExp(regExp);
            return pattern.test(str);
        } catch (e) {
            return false;
        }
    }
    IIRPA.getIeVersion = function () {
        if (IIRPA.ieVersion) {
            return IIRPA.ieVersion;
        }
        var rv = -1;
        if (navigator.appName == 'Microsoft Internet Explorer') {
            var ua = navigator.userAgent;
            var re = new RegExp("MSIE ([0-9]{1,}[\.0-9]{0,})");
            if (re.exec(ua) != null)
                rv = parseFloat(RegExp.$1);
        } else if (navigator.appName == 'Netscape') {
            var ua = navigator.userAgent;
            var re = new RegExp("Trident/.*rv:([0-9]{1,}[\.0-9]{0,})");
            if (re.exec(ua) != null)
                rv = parseFloat(RegExp.$1);
        }
        if (rv == -1 && !!document.documentMode) {
            rv = document.documentMode;
        }
        IIRPA.ieVersion = rv;
        return rv;
    }
    IIRPA.JSON = function () {
        return window.JSON2 || JSON;
    }
    IIRPA.trace = function (message) {
        if (IIRPA.traceEnabled) {
            try {
                console.log(message);
            } catch (e) { }
        }
    }

    IIRPA.cssPathEscape = function (value) {
        if (arguments.length == 0) {
            throw new TypeError('`CSS.escape` requires an argument.');
        }
        var string = String(value);
        var length = string.length; var index = -1;
        var codeUnit;
        var result = '';
        var firstCodeUnit = string.charCodeAt(0);
        while (++index < length) {
            codeUnit = string.charCodeAt(index);
            // Note: there’s no need to special-case astral symbols, surrogate  
            // pairs, or lone surrogates.
            // If the character is NULL (U+0000), then the REPLACEMENT CHARACTER    
            // (U+FFFD).    
            if (codeUnit == 0x0000) {
                result += '\uFFFD'; continue;
            } if (
                // If the character is in the range [\\1-\\1F] (U+0001 to U+001F) or is             
                // U+007F, […](codeUnit >= 0x0001 && codeUnit <= 0x001F) || codeUnit == 0x007F ||
                // If the character is the first character and is in the range [0-9]           
                // (U+0030 to U+0039), […]             
                (index == 0 && codeUnit >= 0x0030 && codeUnit <= 0x0039) ||
                // If the character is the second character and is in the range [0-9]       
                // (U+0030 to U+0039) and the first character is a `-` (U+002D), […]            
                (index == 1 && codeUnit >= 0x0030 && codeUnit <= 0x0039 && firstCodeUnit == 0x002D)) {
                // https://drafts.csswg.org/cssom/#escape-a-character-as-code-point           
                result += '\\' + codeUnit.toString(16) + ' '; continue;
            } if (
                // If the character is the first character and is a `-` (U+002D), and             
                // there is no second character, […]             
                index == 0 && length == 1 && codeUnit == 0x002D) {
                result += '\\' + string.charAt(index);
                continue;
            }
            // If the character is not handled by one of the above rules and is         
            // greater than or equal to U+0080, is `-` (U+002D) or `_` (U+005F), or         
            // is in one of the ranges [0-9] (U+0030 to U+0039), [A-Z] (U+0041 to         
            // U+005A), or [a-z] (U+0061 to U+007A), […]         
            if (codeUnit >= 0x0080 || codeUnit == 0x002D || codeUnit == 0x005F || codeUnit >= 0x0030 && codeUnit <= 0x0039 || codeUnit >= 0x0041 && codeUnit <= 0x005A || codeUnit >= 0x0061 && codeUnit <= 0x007A) {
                // the character itself           
                result += string.charAt(index); continue;
            }
            // Otherwise, the escaped character.
            // https://drafts.csswg.org/cssom/#escape-a-character   
            result += '\\' + string.charAt(index);
        }
        return result;
    };
    //dom操作工具类
    IIRPA.dom.NodeType = {
        ELEMENT: 1,
        ATTRIBUTE: 2,
        TEXT: 3,
        CDATA_SECTION: 4,
        ENTITY_REFERENCE: 5,
        ENTITY: 6,
        PROCESSING_INSTRUCTION: 7,
        COMMENT: 8,
        DOCUMENT: 9,
        DOCUMENT_TYPE: 10,
        DOCUMENT_FRAGMENT: 11,
        NOTATION: 12
    };
    IIRPA.dom.Attr = {
        TAG: 'tag',
        ID: 'id',
        NAME: 'name',
        HREF: 'href',
        CLASS: 'class',
        SRC: 'src',
        TYPE: 'type',
        VALUE: 'value',
        TITLE: 'title',
        CONTENTEDITABLE: 'contenteditable',
        ROWSPAN: 'rowspan',
        COLSPAN: 'colspan',
        HEIGHT: 'height',
        HIDDEN: 'hidden',
        READONLY: 'readonly',
        TEXT: 'innertext',
        OUTERTEXT: 'outertext',
        NTH_CHILD: '@index',// nth-child
        INDEX: 'index',
        RPAGUID: 'i-rpa-guid',
        ELEVISIBLE: 'elevisible'
    }
    IIRPA.dom.Event = {
        CLICK: 'click',
        DBLCLICK: 'dblclick',
        MOUSEDOWN: 'mousedown',
        MOUSEUP: 'mouseup',
        MOUSEOVER: 'mouseover',
        MOUSEMOVE: 'mousemove',
        FOCUS: 'focus',
        CHANGE: 'change'
    }
    IIRPA.dom.vaildHtmlElement = function (node) {
        try {
            if (!node || node.nodeType !== IIRPA.dom.NodeType.ELEMENT) {
                return false;
            }
        } catch (e) {
            IIRPA.Error(IIRPA.ErrorCode.INVALID_PARAM, 'node nodeType is invalid');
        }
        return true;
    }
    /** 
     * ie7,8不支持createEvent  
     * ie 11不支持fireEvent  
     * @param {*} node   
     * @param {*} eventName  
     */
    IIRPA.dom.dispatchEvent = function (node, eventName) {
        try {
            //fireevent need on         
            node.fireEvent("on" + eventName);
        } catch (e) {
            try {
                var event = document.createEvent("HTMLEvents");
                event.initEvent(eventName, true, false);
                node.dispatchEvent(event);
            } catch (e) { }
        }
    }
    IIRPA.dom.lowerCaseNodeName = function (node) {
        function isForm(node) {
            try {
                return node instanceof HTMLFormElement;
            } catch (e) {
                //ie8 以下 作为是否为form标签的判断             
                return node.getAttribute("method") != "" && node.getAttribute("method") != null && node.getAttribute("method") != undefined;
            }
        }
        if (isForm(node)) {
            return 'form';
        } else {
            return node && node.nodeName.toLowerCase();
        }
    }
    IIRPA.dom.querySelectorCount = function (selector, parent) {
        var nodes = IIRPA.dom.querySelectorAll(selector, parent);
        return nodes ? nodes.length : 0;
    }
    IIRPA.dom.querySelectorAll = function (selector, parent) {
        parent = parent || document;
        var usingSizzle = !!document.documentMode && IIRPA.getIeVersion() < 9 && selector.indexOf('nth-child') > -1;
        if (IIRPA.dom.canUseQuerySelector(parent) && !usingSizzle) {
            try {
                IIRPA.trace("querySelectorAll:" + selector);
                return parent.querySelectorAll(selector);
            } catch (e) {
                throw new IIRPA.Error(IIRPA.ErrorCode.INVALID_CSS_SELECTOR, 'querySelector:' + selector + ' is invalid');
            }
        } else {
            IIRPA.trace("querySelecorAllUsingSizzle:" + selector);
            return IIRPA.dom.querySelecorAllUsingSizzle(selector, parent);
        }
    }
    IIRPA.dom.querySelector = function (selector, parent) {
        return (IIRPA.dom.querySelectorAll(selector, parent) || [])[0];
    };
    IIRPA.dom.querySelecorAllUsingSizzle = function (selector, parent) {
        try {
            var nodes = []
            //验证结果 firefox不支持这个函数 
            Sizzle(selector, parent, nodes);
            return nodes;
        } catch (e) {
            throw new IIRPA.Error(IIRPA.ErrorCode.INVALID_CSS_SELECTOR, 'sizzle:' + selector + ' is invalid');
        }
    }
    //ie7 can't
    IIRPA.dom.canUseQuerySelector = function (parent) {
        parent = parent || document;
        return !!(parent.querySelectorAll && parent.querySelector);
    }
    IIRPA.dom.nodeAttributeName = function (node) {
        var attributes = node.attributes;
        var length = attributes.length;
        var result = [];
        for (var i = 0; i < length; i++) {
            if (attributes[i].value.length > 0)
                result.push(attributes[i].name);
        }
        return result;
    }
    //判断某节点是否有某属性值
    IIRPA.dom.nodeHasAttribute = function (node, name) {
        //ie7无hasAttribute方法   
        return document.documentElement.hasAttribute ? node.hasAttribute(name) : !!(node.attributes[name] && node.attributes[name].specified);
    }
    //获取某个node上,属性的名称
    IIRPA.dom.attrValue = function (node, name) {
        switch (name.toLowerCase()) {
            case IIRPA.dom.Attr.ID:
                return node.getAttribute('id');
            case IIRPA.dom.Attr.NAME:
                return node.getAttribute('name');
            case IIRPA.dom.Attr.CLASS:
                return node.getAttribute('class') || node.className;
            case IIRPA.dom.Attr.TYPE:
                return node.getAttribute('type');
            //注意非node.type         
            case IIRPA.dom.Attr.VALUE:
                return node.value;
            case IIRPA.dom.Attr.SRC:
                return node.src;
            case IIRPA.dom.Attr.HREF:
                return node.href;
            case IIRPA.dom.Attr.TITLE:
                return node.title;
            case IIRPA.dom.Attr.TEXT:
                var text = node.innerText || node.outerText
                return IIRPA.trim(text);
            case IIRPA.dom.Attr.OUTERTEXT:
                var text = node.outerText
                return IIRPA.trim(text);
            case IIRPA.dom.Attr.READONLY:
                return node.readOnly;
            case IIRPA.dom.Attr.ELEVISIBLE:
                return (!IIRPA.dom.IsElementHidden(node, true)).toString()
            default:
                return node.getAttribute(name);
        }
    }
    //收集node的信息
    IIRPA.dom.getNodeOptionProps = function (node, isNeedNthChild) {
        if (!node || node.nodeType !== 1) {
            return [];
        }
        var props = [];
        isNeedNthChild = isNeedNthChild || false;
        var attrNames = IIRPA.dom.nodeAttributeName(node);
        var nodeName = IIRPA.dom.lowerCaseNodeName(node);
        for (var i = 0; i < attrNames.length; i++) {
            var attrName = attrNames[i];
            var attrValue = node.getAttribute(attrName);
            if (attrValue == null || attrValue === '' || attrValue === undefined || attrValue.length > 500) {
                continue;
            }
            if (attrName === "i-rpa" || attrName === "z-rpa" || attrName === "style")
                continue;
            if (attrName === "id" && IIRPA.dom.checkIdUnique(nodeName, attrValue, IIRPA.dom.getRootNode(node))) {
                props.push(IIRPA.buildProp(IIRPA.dom.Attr.ID, attrValue, IIRPA.PropPattern.EQUAL, false));
                continue;
            }
            if (attrName === "type" && IIRPA.dom.lowerCaseNodeName(node) == 'input') {
                props.push(IIRPA.buildProp(IIRPA.dom.Attr.TYPE, attrValue, IIRPA.PropPattern.EQUAL, false));
                continue;
            }
            if (attrName === "name") {
                props.push(IIRPA.buildProp(IIRPA.dom.Attr.NAME, attrValue, IIRPA.PropPattern.EQUAL, false));
                continue;
            }
            if (attrName === "class") {
                props.push(IIRPA.buildProp(IIRPA.dom.Attr.CLASS, attrValue, IIRPA.PropPattern.EQUAL, false));
                continue;
            }
            if (attrName === "src" && IIRPA.indexOfArray(["iframe", "frame"], nodeName) !== -1) {
                if (node.src && typeof (node.src) === "string") {
                    var src = node.src;
                    if (IIRPA.dom.frameElement(node)) {
                        src = IIRPA.dom.truncSrcAttr(src);
                    }
                    props.push(IIRPA.buildProp(IIRPA.dom.Attr.SRC, src, IIRPA.PropPattern.CONTAIN, false));
                }
                continue;
            }
            if (attrName === IIRPA.dom.Attr.RPAGUID) {
                props.push(IIRPA.buildProp(attrName, attrValue, IIRPA.PropPattern.EQUAL, true));//数据采集时使用，默认启用
                continue;
            }
            props.push(IIRPA.buildProp(attrName, attrValue, IIRPA.PropPattern.EQUAL, false));
        }
        if (IIRPA.dom.getChildCount(node) === -1) {
            var text = node.innerText || node.outerText
            if (text && IIRPA.trim(text).length < 20) {
                props.push(IIRPA.buildProp(IIRPA.dom.Attr.TEXT, IIRPA.trim(text), IIRPA.PropPattern.EQUAL, false));
            }
        }
        if (isNeedNthChild) {
            var nthChild = IIRPA.dom.getNthChild(node);
            if (nthChild > -1) {
                props.push(IIRPA.buildProp(IIRPA.dom.Attr.NTH_CHILD, nthChild, IIRPA.PropPattern.EQUAL, false));
            }
        }
        return props;
    }
    IIRPA.dom.getChildCount = function (node) {
        var children = node.children;
        if (!children || children.length == 0)
            return -1;
        return children.length;
    }
    IIRPA.dom.getNthChildCount = function (node) {
        var parent = node.parentNode;
        if (!parent)
            return -1;
        var siblings = parent.children;
        if (!siblings || siblings.length == 0)
            return -1;
        return siblings.length;
    }
    /**  * 计算某个节点的nth-child值  * @param {*} node   */
    IIRPA.dom.getNthChild = function (node) {
        var parent = node.parentNode;
        if (!parent)
            return -1;
        var siblings = parent.children;
        if (!siblings || siblings.length == 0)
            return -1;
        var ownIndex = 0;
        for (var i = 0; i < siblings.length; i++) {
            var sibling = siblings[i];
            if (!IIRPA.dom.vaildHtmlElement(sibling)) {
                continue;
            }
            ownIndex += 1;
            if (sibling == node) {
                break;
            }
        }
        return ownIndex;
    }
    /**  * 获取元素的属性值列表，包括属性名、是否有某属性、值是否为空  * @param {*} node   * @param {*} propArray   */
    IIRPA.dom.nodePropArray = function (node, propArray) {
        propArray = propArray || ['id', 'name', 'class'];
        var propValueArray = [];
        for (var i = 0; i < propArray.length; i++) {
            var propName = propArray[i];
            var propValueObj = { name: propName };
            if (IIRPA.dom.nodeHasAttribute(node, propName)) {
                propValueObj.has = true;
                var propValue = node.getAttribute(propName);
                if (propValue) {
                    propValueObj.empty = false;
                } else {
                    propValueObj.empty = true;
                }
            } else {
                propValueObj.has = false;
            }
            propValueArray.push(propValueObj);
        }
        return propValueArray;
    }
    /**  * 找元素树  * @param {*} node   * @param {*} maxLevel   */
    IIRPA.dom.serachTree = function (node, maxLevel) {
        var nodeTree = [];
        var contextNode = node;
        while (maxLevel > -1 && contextNode) {
            nodeTree.push(contextNode);
            contextNode = contextNode.parentNode;
            maxLevel--;
        }
        return nodeTree;
    }
    /**  * 获取node的主干path树,并收集树每个上的可选属性：type id name class text(最底层且长度不超过15),nth-child  * @param {*} node   * @param {*} optimize   */
    IIRPA.dom.truncPathTree = function (node, optimize) {
        optimize = optimize || false;
        if (!IIRPA.dom.vaildHtmlElement(node) && !IIRPA.dom.isShadowRootNode(node)) {
            throw new IIRPA.Error(IIRPA.ErrorCode.INVALID_PARAM, 'node must be elementNode');
        }
        var pathTree = [];
        var contextNode = node;
        var firstUniqueEle = null;
        var firstUniqueEleIdx = -1;
        var level = -1;
        while (contextNode) {
            if (IIRPA.dom.isShadowRootNode(contextNode))
                break;
            var nodeName = IIRPA.dom.lowerCaseNodeName(contextNode);
            if (nodeName == "#document") {
                break;
            }
            //如果只有body还需要加的      
            if ((nodeName == 'body' || nodeName == "html") && pathTree.length > 0)
                break;
            //收集可选属性列表       
            var params = [];
            var optionProps = IIRPA.dom.getNodeOptionProps(contextNode, true);
            level++;
            if (optimize && IIRPA.dom.usefulId(contextNode) && IIRPA.dom.checkIdUnique(nodeName, contextNode.getAttribute('id'), IIRPA.dom.getRootNode(contextNode))) {
                //add id param                
                if (firstUniqueEleIdx === -1) {
                    firstUniqueEle = contextNode;
                    firstUniqueEleIdx = level;
                }
                params.push(IIRPA.buildProp(IIRPA.dom.Attr.ID, contextNode.getAttribute('id'), IIRPA.PropPattern.EQUAL, true));
            }
            //有type也可以加进来
            if (nodeName == 'input' && node.getAttribute('type')) {
                params.push(IIRPA.buildProp(IIRPA.dom.Attr.TYPE, contextNode.getAttribute('type'), IIRPA.PropPattern.EQUAL, true));
            }
            //iframe且有唯一的src,也需要加入
            if (IIRPA.dom.frameElement(contextNode) && IIRPA.dom.uniqueFrameSrc(contextNode)) {
                if (firstUniqueEleIdx === -1) {
                    firstUniqueEle = contextNode;
                    firstUniqueEleIdx = level;
                }
                params.push(IIRPA.buildProp(IIRPA.dom.Attr.SRC, IIRPA.dom.truncSrcAttr(contextNode.src), IIRPA.PropPattern.CONTAIN, true));
            }
            //不是唯一元素，只有一个nth-child 且大于 1 也要加上 
            if (firstUniqueEleIdx != level && optionProps.length === 1 && optionProps[0].value > 1 && (optionProps[0].name == 'nth-child' || optionProps[0].name == IIRPA.dom.Attr.NTH_CHILD))
                params.push(IIRPA.buildProp(optionProps[0].name, optionProps[0].value, IIRPA.PropPattern.EQUAL, true));

            pathTree.push(IIRPA.buildPath(nodeName, params, optionProps));
            contextNode = contextNode.parentNode;

        }
        pathTree.reverse();
        if (firstUniqueEleIdx > -1) {
            firstUniqueEleIdx = pathTree.length - 1 - firstUniqueEleIdx;
        }
        return {
            pathTree: pathTree,
            firstUniqueEle: firstUniqueEle,
            firstUniqueEleIdx: firstUniqueEleIdx
        };
    }
    /**  * 获取某个节点的css全路径，即是 nodeName(nth-child)的形式,若只有一个child不需要  */
    IIRPA.dom.cssAllPath = function (node) {
        if (!(node instanceof Element))
            return;
        var contextNode = node;
        var paths = [];
        while (contextNode && contextNode.parentNode) {
            var nodeName = IIRPA.dom.lowerCaseNodeName(contextNode);
            if ((nodeName == 'body' || nodeName == 'html') && paths.length > 0)
                break;
            var path = nodeName;
            //find child        
            var child = IIRPA.dom.getNthChild(contextNode);
            //if (contextNode.parent.children.length > 1) {
            path += ":nth-child(" + child + ")";
            //}
            paths.push(path);
            contextNode = contextNode.parentNode;
            if (nodeName == 'html')
                break;
        }
        return paths;
    }
    IIRPA.dom.getRootNode = function (node) {
        return node && node.getRootNode ? node.getRootNode() : null;
    }
    //校验id是否唯一
    IIRPA.dom.checkIdUnique = function (nodeName, id, rootNode) {
        if (IIRPA.isString(id)) {
            var selector = IIRPA.cssPathEscape(nodeName) + '#' + IIRPA.escapeIdentifierIfNeeded(id);
            var count = IIRPA.dom.querySelectorCount(selector, rootNode);
            return count == 1;
        }
        return false;
    }
    //是否有不含数字的id
    IIRPA.dom.usefulId = function (node) {
        return node && node.getAttribute('id') && !/[0-9]/.test(node.getAttribute('id'));
    }
    IIRPA.dom.usefulIdValue = function (value) {
        return value && !/[0-9]/.test(value);
    }
    /**  * 多个空格隔开的class转成数组  * @param {*} className   */
    IIRPA.dom.convertClassToArray = function (className) {
        var classArray = IIRPA.trim(className).split(' ');
        //去除空数组    
        for (var j = classArray.length; j >= 0; j--) {
            if (classArray[j] == '') {
                classArray.splice(j, 1);
            }
        }
        return classArray;
    }
    IIRPA.dom.frameElement = function (node) {
        var nodeName = IIRPA.dom.lowerCaseNodeName(node);
        return nodeName == 'iframe' || nodeName == 'frame';
    }
    /**  * iframe的src是否唯一  */
    IIRPA.dom.uniqueFrameSrc = function (node) {
        if (node.src) {
            var src = IIRPA.dom.truncSrcAttr(node.src);
            //css中src只能是源码里src=\"\"的值，而node.src会加上域名等信息级成实际的页面         
            //var selector = node.nodeName+'[src^=\"'+src+'\"]';        
            //return IIRPA.dom.querySelectorCount(selector) == 1;         
            var frames = document.getElementsByTagName(node.nodeName);
            var count = 0;
            for (var i = 0; i < frames.length; i++) {
                var frameNode = frames[i];
                if (frameNode.src.indexOf(src) > -1)
                    count++;
                if (count > 1) break;
            }
            return count == 1;
        }
        return false;
    }
    /**  * 截取src ? 前面部分  */
    IIRPA.dom.truncSrcAttr = function (src) {
        var index = src.indexOf('?');
        if (index < 0) { index = src.length; }
        return src.substr(0, index);
    }
    IIRPA.dom.boundingClientRectList = function (nodes) {
        if (nodes) {
            var clientRectList = [];
            for (var i = 0; i < nodes.length; i++) {
                clientRectList.push(IIRPA.dom.boundingClientRect(nodes[i]));
            }
            return clientRectList;
        }
    }
    IIRPA.dom.textList = function (nodes) {
        if (nodes) {
            var textList = [];
            for (var i = 0; i < nodes.length; i++) {
                var text = nodes[i].innerText || nodes[i].outerText
                textList.push(IIRPA.trim(text || nodes[i].value));
            }
            return textList;
        }
    }  /**  * 获取元素的坐标  * @param {*} nodes   */
    IIRPA.dom.boundingClientRect = function (node) {
        if (node) {
            var clientRect = node.getBoundingClientRect();
            return {
                left: clientRect.left,
                top: clientRect.top,
                width: clientRect.right - clientRect.left,
                height: clientRect.bottom - clientRect.top
            }
        }
    }
    /**  * 判断元素是否有x滚动条  * @param {*} node   */
    IIRPA.dom.OverFlowXNode = function (node) {
        if (node) {
            if (!node.currentStyle || !node.currentStyle.overflowX) {
                return false;
            }
            var overflowX = node.currentStyle.overflowX;
            return overflowX == "auto" || overflowX == "scroll";
        }
        return false;
    }
    IIRPA.dom.OverFlowYNode = function (node) {
        if (node) {
            if (!node.currentStyle || !node.currentStyle.overflowY) {
                return false;
            }
            var overflowY = node.currentStyle.overflowY;
            return overflowY == "auto" || overflowY == "scroll";
        }
        return false;
    }
    IIRPA.dom.IsElementHidden = function (ele, includedAncestors) {
        if (!ele) {
            return true;
        }
        var style = IIRPA.dom.getComputedStyle(ele);
        if (!style) {
            return true;
        }
        if (
            style.display === 'none' || // display: none
            style.visibility === 'hidden' || // visibility: hidden
            style.opacity === '0' || // opacity: 0
            (!ele.shadowRoot && ele.offsetWidth === 0 && ele.offsetHeight === 0) // 宽高均为 0
        ) {
            return true;
        }
        if (includedAncestors) {
            // 检查父级元素是否隐藏
            var parent = ele.parentNode;
            while (parent && parent.nodeType == 1) {
                var parentStyle = IIRPA.dom.getComputedStyle(parent);
                if (!parentStyle) {
                    break;
                }
                if (
                    parentStyle.display === 'none' ||
                    parentStyle.visibility === 'hidden' ||
                    parentStyle.opacity === '0'
                ) {
                    return true;
                }
                parent = parent.parentNode;
            }
        }
        return false;
    }
    IIRPA.dom.getComputedStyle = function (ele) {
        var style;
        if (window.getComputedStyle) {
            style = window.getComputedStyle(ele);
        } else if (ele.currentStyle) {
            style = ele.currentStyle;
        } else {
            style = null;
        }
        return style;
    }
    /**  * 判断某个元素是否在可视区域内   *     - 若不指定坐标，要是整个元素都在可见区域内  *     - 否则，坐标在可见区域内即可  * @param {HTMLElement} node   * @param {int} elementLeft  * @param {int} elementTop  */
    IIRPA.dom.elementVisibleLocation = function (node, location, frameLeft, frameTop) {
        var windowSize = IIRPA.dom.clientSize();
        var frameLeft = IIRPA.ParseInt(frameLeft);
        var frameTop = IIRPA.ParseInt(frameTop);
        //第一步：在屏幕可视区域内  
        location = location || "center";
        if (!locationInView(node, location)) {
            IIRPA.trace("Node has already in view.");
            return false;
        }
        //第二步：没有因overflow 遮挡在滚动条内
        var overFlowParent = getOverFlowParent(node);
        if (overFlowParent) {
            IIRPA.trace("Node's parent has scroll");
            var parentRect = IIRPA.dom.boundingClientRect(overFlowParent);
            var nodeRect = IIRPA.dom.boundingClientRect(node);
            //计算node 相对于overFlowParent的偏移坐标      
            var offsetParentLeft = nodeRect.left - parentRect.left;
            var offsetParentTop = nodeRect.top - parentRect.top;
            //有X滚动条       
            if (IIRPA.dom.OverFlowXNode(overFlowParent)) {
                IIRPA.trace("Node's parent has X-scroll");
                //offsetParentLeft<0,说明在滚动条左边         
                if (offsetParentLeft < 0) {
                    IIRPA.trace("Node hides in scroll-left");
                    return false;
                }
                //offsetParentLeft > overFlowParent.clientWidth说明在滚动条右边
                if (offsetParentLeft > parseInt(overFlowParent.clientWidth)) {
                    IIRPA.trace("Node hides in scroll-right");
                    return false;
                }
            }
            //有Y滚动条
            if (IIRPA.dom.OverFlowYNode(overFlowParent)) {
                IIRPA.trace("Node's parent has Y-scroll");
                // offsetParentTop < 0 说明在滚动条上方         
                if (offsetParentTop < 0) {
                    IIRPA.trace("Node hides in scroll-top");
                    return false;
                }
                //offsetParentTop > overFlowParent.clientHeight说明在滚动条下方      
                if (offsetParentTop > parseInt(overFlowParent.clientHeight)) {
                    IIRPA.trace("Node hides in scroll-bottom");
                    return false;
                }
            }
        }
        return true;
        /**      * 获取有overflow的父亲，不能为body      * @param {*} node       */
        function getOverFlowParent(node) {
            var contextNode = node;
            while (contextNode) {
                var nodeName = IIRPA.dom.lowerCaseNodeName(contextNode);
                if (nodeName == 'body')
                    break;
                if (IIRPA.dom.OverFlowXNode(contextNode) || IIRPA.dom.OverFlowYNode(contextNode)) {
                    return contextNode;
                }
                contextNode = contextNode.parentNode;
            }
        }
        function locationInView(node, location) {
            var clientRect = IIRPA.dom.boundingClientRect(node);
            switch (location) {
                case IIRPA.Location.UPPERLEFT:
                    //左上角     
                    if (!positionInView(clientRect.left + frameLeft, clientRect.top + frameTop)) {
                        return false;
                    }
                    break;
                case IIRPA.Location.UPPERRIGHT:
                    //右上角      
                    var clientRight = clientRect.left + clientRect.width;
                    var clientBottom = clientRect.top;
                    if (!positionInView(clientRight + frameLeft, clientBottom + frameTop)) {
                        return false;
                    }
                    break;
                case IIRPA.Location.LOWERRIGHT:
                    //右下角      
                    var clientRight = clientRect.left + clientRect.width;
                    var clientBottom = clientRect.top + clientRect.height;
                    if (!positionInView(clientRight + frameLeft, clientBottom + frameTop)) {
                        return false;
                    }
                    break;
                case IIRPA.Location.LOWERLEFT:
                    //左下角      
                    var clientRight = clientRect.left;
                    var clientBottom = clientRect.top + clientRect.height;
                    if (!positionInView(clientRight + frameLeft, clientBottom + frameTop)) {
                        return false;
                    }
                    break
                case IIRPA.Location.CENTER:
                    //中心     
                    if (!positionInView(clientRect.width / 2 + clientRect.left + frameLeft, clientRect.height / 2 + clientRect.top + frameTop)) {
                        return false;
                    }
                    break;
                default:
                    //IIRPA.Location.RANDOM
                    //左上角
                    if (!positionInView(clientRect.left + frameLeft, clientRect.top + frameTop)) {
                        return false;
                    }
                    //右下角
                    var clientRight = clientRect.left + clientRect.width;
                    var clientBottom = clientRect.top + clientRect.height;
                    if (!positionInView(clientRight + frameLeft, clientBottom + frameTop)) {
                        return false;
                    }
            }
            return true;
        }

        function positionInView(x1, y1) {
            //-X 在屏幕外面      
            if (x1 < 0 || x1 > windowSize.width) {
                return false;
            }
            //-Y 在屏幕外面      
            if (y1 < 0 || y1 > windowSize.height) {
                return false;
            }
            return true;
        }
    }
    /**  * 判断两个node是否为父子关系  * @param {*} parent   * @param {*} child   */
    IIRPA.dom.parentAndChild = function (parent, child) {
        if (parent && child) {
            var node = child.parentNode;
            while (node) {
                if (node == parent) {
                    return true;
                }
                node = node.parentNode;
            }
        }
        return false;
    }
    /**  * 调用完node.scrollIntoView()后，计算有无遮挡的情况：  *   若scene='click',只需要检查中心点  *   否则，要整个控件都未被遮挡，目前先通过检查2角，左上角+右下角来实现  * @param {*} node   * @param {*} scene   */
    IIRPA.dom.scrollIntoView = function (node, scene) {
        //do scroll    
        node.scrollIntoView();
        //check     
        var checkType = checkType();
        var coverNode;
        var rect = IIRPA.dom.boundingClientRect(node);
        switch (checkType) {
            case 1:
                var leftCorner = new IIRPA.Coordinate(rect.left, rect.top);
                var rightCorner = new IIRPA.Coordinate(rect.left + rect.width, rect.top + rect.height);
                if (!CoverNode(node, leftCorner) && !CoverNode(node, rightCorner)) {
                    return;
                }
                break;
            case 2:
                var centerPoint = new IIRPA.Coordinate(rect.left + rect.width / 2, rect.top + rect.height / 2);
                if (!CoverNode(node, centerPoint)) {
                    return;
                }
                break;
        }
        //有coverNode,移动时，加一点偏差 + 5px;
        if (coverNode) {
            var offsetHeight = 5;
            var coverRect = IIRPA.dom.boundingClientRect(coverNode);
            var nodeOffsetToCoverTop = rect.top - coverRect.top;
            //node在coverNode内，或上半身在coverNode中，此时需要页面滚动条上移，coverRect.height-nodeOffsetToCoverTop     
            if (nodeOffsetToCoverTop >= 0) {
                IIRPA.trace("Node's top is covered");
                var scrollHeight = coverRect.height - nodeOffsetToCoverTop + offsetHeight;
                IIRPA.dom.scrollTop(null, scrollHeight * -1);
            } else {
                //上半身在coverNode中，此时需要页面滚动条上移，rect.height - Math.abs(nodeOffsetToCoverTop)
                var scrollHeight = rect.height - Math.abs(nodeOffsetToCoverTop) + offsetHeight;
                var scrollableTop = IIRPA.dom.scrollableSize().height;
                //node已在上面，无法再向前翻动，只能移动到下方        
                if (scrollableTop < Math.abs(scrollHeight)) {
                    IIRPA.trace("Node is topest, need scrolls to bottom.");
                    scrollHeight = coverRect.height + Math.abs(nodeOffsetToCoverTop) + offsetHeight;
                    IIRPA.dom.scrollTop(null, scrollHeight * -1);
                } else {
                    IIRPA.trace("Node's bottom is covered");
                    IIRPA.dom.scrollTop(null, scrollHeight);
                }
            }
        }
        /**      * 检查元素node的坐标(x,y)有无被其它元素遮挡      * @param {*} node       * @param {*} pos      */
        function CoverNode(node, pos) {
            var posNode = document.elementFromPoint(pos.x, pos.y);
            if (node == posNode) { return false; }
            //posNode is not parent   
            //if yes,情况会比较复杂了，可能的情况是，node本身的信息是分散，故(x,y)位置落在非node内，但不代表这种情况下需要再做位置修改        
            if (IIRPA.dom.parentAndChild(posNode, node)) {
                IIRPA.trace("Node is covered in parent, do nothing!");
                return false;
            }
            coverNode = posNode;
            return true;
        }
        /**      * 计算是哪种检查方式，      * type=1,检查控件未被遮挡      * type=2,检查中心点未被遮挡      */
        function checkType() {
            scene = scene || '';
            switch (scene.toLowerCase()) {
                case 'click':
                case 'mouseover':
                    return 2;
                default:
                    return 1;
            }
        }
    }
    IIRPA.dom.getScrollTop = function (node) {
        if (node) {
            if (node == document.documentElement) {
                return window.scrollY || document.documentElement.scrollTop || document.body.scrollTop || 0;
            }
            return node.scrollTop;
        }
        return window.scrollY || document.documentElement.scrollTop || document.body.scrollTop || 0;
    }
    IIRPA.dom.getScrollLeft = function (node) {
        if (node) {
            if (node == document.documentElement) {
                return window.scrollX || document.documentElement.scrollLeft || document.body.scrollLeft || 0;
            }
            return node.scrollLeft;
        }
        return window.scrollX || document.documentElement.scrollLeft || document.body.scrollLeft || 0;
    }
    IIRPA.dom.getClientHeight = function (node) {
        if (node) {
            return node.clientHeight;
        }
        return document.documentElement.clientHeight || document.body.clientHeight || 0;
    }
    IIRPA.dom.getClientWidth = function (node) {
        if (node) {
            return node.clientWidth;
        }
        return document.documentElement.clientWidth || document.body.clientWidth || 0;
    }
    IIRPA.dom.scrollTop = function (node, height) {
        /*node = node || document.documentElement;*/
        //考虑值为0的情况下不需要做值的转换
        if (height == null || height == undefined) {
            height = IIRPA.dom.getClientHeight(node);
        }
        /*      height = height || node.clientHeight;*/

        var beforeTop = IIRPA.dom.getScrollTop(node);
        //scroll to top    
        if (height < 0 && beforeTop == 0) {
            return 0;
        }
        //scroll to buttom ,return 0
        //没明白这个可滚动范围怎么处理的，即使等于0，我可以向上下滚动啊，先将=的情况去掉吧
        //if (height >= 0 && IIRPA.dom.scrollableSize(node).height == 0) {
        if (height > 0 && IIRPA.dom.scrollableSize(node).height == 0) {
            return 0;
        }
        //var scrollLength = parseInt(beforeTop) + parseInt(height);
        var scrollLength = parseInt(height);
        IIRPA.trace("scroll top height:" + scrollLength);
        if (node) {
            node.scrollTop = scrollLength;
        }
        else {
            document.documentElement.scrollTop = scrollLength;
            document.body.scrollTop = scrollLength;
        }
        return 1;
    }
    IIRPA.dom.scrollLeft = function (node, width) {
        //node = node || document.documentElement;
        //考虑值为0的情况下不需要做值的转换
        if (width == null || width == undefined) {
            width = IIRPA.dom.getClientWidth(node);
        }
        /* width = width || node.clientWidth;*/
        var beforeLeft = IIRPA.dom.getScrollLeft(node);
        if (width < 0 && beforeLeft == 0) {
            return 0;
        }
        if (width > 0 && IIRPA.dom.scrollableSize(node).width == 0) {
            return 0;
        }
        //var scrollLength = parseInt(beforeLeft) + parseInt(width);
        var scrollLength = parseInt(width);
        IIRPA.trace("scroll left width:" + scrollLength);
        if (node) {
            node.scrollLeft = scrollLength;
        }
        else {
            document.documentElement.scrollLeft = scrollLength;
            document.body.scrollLeft = scrollLength;
        }
        return 1;
    }
    IIRPA.dom.Screen = function () {
        return { width: screen.availWidth, height: screen.availHeight };
    }
    IIRPA.dom.clientSize = function (node) {
        node = node || document.documentElement;
        return { width: node.clientWidth, height: node.clientHeight };
    }
    IIRPA.dom.scrollSize = function (node) {
        node = node || document.documentElement;
        return { width: node.scrollWidth, height: node.scrollHeight }
    }
    IIRPA.dom.areaInfo = function (node) {
        var scrollWidth, scrollHeight, scrollTop, scrollLeft;
        if (node) {
            scrollWidth = node.scrollWidt;
            scrollHeight = node.scrollHeight;
            scrollTop = node.scrollTop;
            scrollLeft = node.scrollLeft;
        }
        else {
            scrollWidth = document.documentElement.scrollWidth || document.body.scrollWidth;
            scrollHeight = document.documentElement.scrollHeight || document.body.scrollHeight;
            scrollTop = document.documentElement.scrollTop || document.body.scrollTop;
            scrollLeft = document.documentElement.scrollLeft || document.body.scrollLeft;
        }
        var viewWidth = window.innerWidth || document.documentElement.clientWidth || document.body.clientWidth
        var viewHeight = window.innerHeight || document.documentElement.clientHeight || document.body.clientHeight
        //按需追加
        return IIRPA.JSON().stringify({
            scrollWidth: scrollWidth,
            scrollHeight: scrollHeight,
            scrollTop: scrollTop,
            scrollLeft: scrollLeft,
            viewWidth: viewWidth,
            viewHeight: viewHeight
        });
    }
    /**  * 可滚动区域大小计算  */
    IIRPA.dom.scrollableSize = function (node) {
        var scrollSize = IIRPA.dom.scrollSize(node) || { width: 0, height: 0 };
        var clientSize = IIRPA.dom.clientSize(node) || { width: 0, height: 0 };
        return {
            width: scrollSize.width - clientSize.width - IIRPA.dom.getScrollLeft(node),
            height: scrollSize.height - clientSize.height - IIRPA.dom.getScrollTop(node)
        };
    }
    IIRPA.dom.tables = function (parent) {
        parent = parent || document;
        return parent.getElementsByTagName('table');
    }
    IIRPA.dom.tableData = function (tableEle, returnType) {
        if (tableEle) {
            var tableData = [];
            var rowData = [];
            var rowSpanDatas = [];
            var hasTbHead = false;
            //rowspan的相关信息         
            var rows = tableEle.rows;
            var _colInx = 0;
            returnType = returnType || 'text';
            for (var item = 0; item < rows.length; item++) {
                rowData = [];
                var row = rows[item]

                if (row.cells.length == 0) {
                    continue;
                }

                var tds = row.cells;
                //找到匹配到的rowspan信息           
                var datasFromRowSpan = [];
                if (rowSpanDatas.length > 0) {
                    datasFromRowSpan = tdsFromRowSpan(item, rowSpanDatas)
                }
                _colInx = 0;
                //不算rowspan的列序号          
                var preRowData = rowData;
                rowData = dataFromTdRowSpan(datasFromRowSpan, 1, 0, rowData, rowSpanDatas);
                _colInx += rowData.length - preRowData.length;
                for (var tdItem = 0; tdItem < tds.length; tdItem++) {
                    preRowData = rowData;
                    rowData = dataFromTdRowSpan(datasFromRowSpan, 2, _colInx, preRowData, rowSpanDatas);
                    _colInx += rowData.length - preRowData.length;
                    var v = ""
                    switch (returnType) {
                        case 'text':
                            var tdChildren = tds[tdItem].children;
                            if (tdChildren.length == 1 && tdChildren[0].tagName.toLowerCase() == "input") {
                                v = IIRPA.trim(tdChildren[0].value)
                            }
                            else {
                                v = IIRPA.trim(tds[tdItem].outerText);
                            }
                            break;
                        case 'html': v = IIRPA.trim(tds[tdItem].outerHTML);
                            break;
                    }
                    rowData.push(v);
                    if (!hasTbHead && tds[tdItem].nodeName === "TH") {
                        hasTbHead = true;
                    }
                    //处理rowspan:                
                    var rowspan = tds[tdItem].rowSpan;
                    if (rowspan != null && rowspan > 1) {
                        rowSpanDatas.push({ col: _colInx, row: item, rowspan: rowspan, value: v });
                    }
                    //处理colspan               
                    var colspan = tds[tdItem].colSpan;
                    if (colspan != null && colspan > 1) {
                        for (var j = 0; j < colspan - 1; j++) {
                            rowData.push("");
                            _colInx++;
                        }
                    }
                    _colInx++;
                }
                //有无尾巴要补       
                preRowData = rowData;
                rowData = dataFromTdRowSpan(datasFromRowSpan, 3, _colInx, preRowData, rowSpanDatas);
                _colInx += rowData.length - preRowData.length;
                tableData.push(rowData);
            }
            if (!hasTbHead && tableData.length > 0) {
                var emptyRowData = [];
                for (var i = 0; i < tableData[0].length; i++) {
                    emptyRowData.push("");
                }
                tableData.unshift(emptyRowData);
            }
            return tableData;
        }
        /**      * 先根据行号计算，此行有几个要处理的rowspan信息      */
        function tdsFromRowSpan(row, rowSpanDatas) {
            var array = [];
            for (var i = 0; i < rowSpanDatas.length; i++) {
                data = rowSpanDatas[i];
                if (data.row >= row)
                    continue;
                if (row - data.row + 1 <= data.rowspan) {
                    array.push({ col: data.col, value: data.value });
                }
            }
            return array;
        }
        /*  
         * 补充rowspan的数据，有三种规则：     
         * 规则1：表格前面列的rowspan
         * 规则2：插入到已有列中间
         * 规则3：尾部合并的rowspan
         */
        function dataFromTdRowSpan(datasFromRowSpan, type, col, rowData, rowSpanDatas) {
            if (datasFromRowSpan.length == 0)
                return rowData;
            switch (type) {
                case 1:
                    var _idx = 0
                    while (true) {
                        var matched = false;
                        for (var i = 0; i < rowSpanDatas.length; i++) {
                            data = rowSpanDatas[i];
                            if (data.col == _idx) {
                                rowData.push(data.value);
                                matched = true;
                                break;
                            }
                        }
                        if (!matched)
                            break;
                        _idx++;
                        //针对本次，处理完前面所有的rowspan行       
                    }
                    break;
                case 2:
                    for (var i = 0; i < rowSpanDatas.length; i++) {
                        data = rowSpanDatas[i];
                        if (data.col == col && col != 0) {
                            rowData.push(data.value);
                            break;
                        }
                    }
                    break;
                case 3:
                    for (var i = 0; i < rowSpanDatas.length; i++) {
                        data = rowSpanDatas[i];
                        if (data.col + 1 > col) {
                            //这里为什么要+1，因为data.col取时从0开始的，而type=3在所有列处理完后，是总数量。所以要加1         
                            rowData.push(data.value);
                        }
                    }
                    break;
            }
            return rowData;
        }
    }
    //找元素方法集体 
    IIRPA.locale.findByUniqueID = function (uniqueID) {
        IIRPA.checkEmpty(uniqueID, 'uniqueID is required');
        var node = document.getElementById(uniqueID);
        if (!node) {
            var nodes = document.getElementsByName(uniqueID);
            if (nodes && nodes.length > 0) {
                node = nodes[0];
            } else {
                throw new IIRPA.Error(IIRPA.ErrorCode.NO_SUCH_ELEMENT, "未找到uniqueID=" + uniqueID);
            }
        }
        return node;
    }
    IIRPA.locale.findNode = function (nodeIdentify) {
        if (nodeIdentify) {
            if (typeof (nodeIdentify) == "string") {
                return IIRPA.locale.findByUniqueID(nodeIdentify);
            }
            return nodeIdentify;
        }
        throw new IIRPA.Error(IIRPA.ErrorCode.NO_SUCH_ELEMENT, "不支持的控件");
    }
    /**  * 元素本身是否有唯一定位的路径,只判断id  * @param {*} node   */
    IIRPA.locale.validPathFromNode = function (node) {
        var nodeName = IIRPA.dom.lowerCaseNodeName(node);
        //针对iframe与frame, src也可作为判断条件  
        var idUnique = IIRPA.dom.usefulId(node) && IIRPA.dom.checkIdUnique(nodeName, node.getAttribute('id'), IIRPA.dom.getRootNode(node));
        var srcUnique = !idUnique && IIRPA.dom.frameElement(node) && IIRPA.dom.uniqueFrameSrc(node);
        if (idUnique || srcUnique) {
            var optionProps = IIRPA.dom.getNodeOptionProps(node);
            var props = [];
            if (idUnique) {
                props.push(IIRPA.buildProp(IIRPA.dom.Attr.ID, node.getAttribute('id'), IIRPA.PropPattern.EQUAL, true));
            } else {
                props.push(IIRPA.buildProp(IIRPA.dom.Attr.SRC, IIRPA.dom.truncSrcAttr(node.src), IIRPA.PropPattern.CONTAIN, true));
            }
            var discernHidden = IIRPA.dom.IsElementHidden(node, true)
            props.push(IIRPA.buildProp(IIRPA.dom.Attr.ELEVISIBLE, (!discernHidden).toString(), IIRPA.PropPattern.EQUAL, false))
            var paths = [];
            paths.push(IIRPA.buildPath(nodeName, props, optionProps));
            return paths;
        }
        return '';
    }
    IIRPA.locale.AllNodeSelector = function (node) {
        //判断路径是否唯一 id 针对iframe与frame, src也可作为判断条件  
        var idUnique = IIRPA.dom.usefulId(node) && IIRPA.dom.checkIdUnique(nodeName, node.getAttribute('id'), IIRPA.dom.getRootNode(node));
        var srcUnique = !idUnique && IIRPA.dom.frameElement(node) && IIRPA.dom.uniqueFrameSrc(node);
        if (idUnique || srcUnique) {
            var paths = [];
            paths.push(node);
            return paths;
        } else {
            return '';
        }
    }
    //trunc_path 过滤时基于的数组
    //id > text(最低层) > class > nth-child(非叶子) 
    IIRPA.locale.filterPropArray = function () {
        var array = [];
        //id    
        array.push({
            name: IIRPA.dom.Attr.ID,
            onLeaf: false,
            onParent: false
        });
        //text     
        array.push({
            name: IIRPA.dom.Attr.TEXT,
            onLeaf: true,
            onParent: false
        });
        //class
        array.push({
            name: IIRPA.dom.Attr.CLASS,
            onLeaf: false,
            onParent: false
        });
        //nth-child    
        array.push({
            name: IIRPA.dom.Attr.NTH_CHILD,
            onLeaf: false,
            onParent: true
        });
        return array;
    }

    IIRPA.locale.validPropsFromPath = function (truncPathTree, inspectNode) {
        // 补充逻辑
        var treeLength = truncPathTree.length;
        for (var i = 0; i < treeLength; i++) {
            if (truncPathTree[i].props && truncPathTree[i].props.length > 1) {
                var cloneProps = IIRPA.JSON().stringify(truncPathTree[i].props);
                if (IIRPA.locale.switchPropsAccurate(truncPathTree[i].props)) {
                    var targetElements = IIRPA.locale.findElements(truncPathTree, undefined, Infinity);
                    if (targetElements.length != 1 || targetElements[0] != inspectNode) {
                        truncPathTree[i].props = IIRPA.JSON().parse(cloneProps);
                    }
                }
            }
        }
    }
    IIRPA.locale.getNodeProp = function (props, name) {
        for (var j = 0; j < props.length; j++) {
            if (props[j].name == name)
                return props[j];
        }
        return null;
    }
    IIRPA.locale.switchPropsAccurate = function (props) {
        var classProp = IIRPA.locale.getNodeProp(props, IIRPA.dom.Attr.CLASS);
        var indexProp = IIRPA.locale.getNodeProp(props, IIRPA.dom.Attr.NTH_CHILD);
        if (classProp && indexProp) {
            if (classProp.name == IIRPA.dom.Attr.CLASS && indexProp.name == IIRPA.dom.Attr.NTH_CHILD) {
                var calssValue = classProp.value;
                if (classProp.accurate == "false")
                    return false;
                if (IIRPA.regMatch(calssValue, "^[a-zA-Z\s]+$"))
                    return false;
                classProp.accurate = "false";
                indexProp.accurate = "true";
            }
            return true;
        }
        return false;
    }
    /**  * 找node在document内唯一路径  * @param {*} inspectNode   */
    IIRPA.locale.extractUniquePath = function (inspectNode, truncPathTree, options) {
        options = options || {};

        if (options.rawData == true) {
            return truncPathTree;
        }
        // TODO: 强制指定根节点
        var gen = new IIRPA.SelectorGenerateor(inspectNode, {
            innerTextFirst: options.innerTextFirst,
            excludeInnerText: options.excludeInnerText,
            ignoreIdAnchor: options.ignoreIdAnchor,
            excludeHiddenEle: options.excludeHiddenEle
        });
        var selector = gen.gen(truncPathTree);

        selector.transTree(truncPathTree);
        var discernHidden = IIRPA.dom.IsElementHidden(inspectNode, true)
        //check truncPathTree unique path
        var targetElements = IIRPA.locale.findElements(truncPathTree, undefined, Infinity, discernHidden);
        if (!targetElements || targetElements.length == 0) {
            //若发生，说明findElements有bug或页面发了变化       
            IIRPA.trace("extractUniquePath : not found any element.");
            return truncPathTree;
        }
        if (targetElements.length == 1) {
            IIRPA.locale.validPropsFromPath(truncPathTree, inspectNode);
            return truncPathTree;
        }
        //第一轮 补属性 diff路径,从最下层开始,nth-child()    
        if (stepOneWithAddPathProp()) {
            IIRPA.locale.validPropsFromPath(truncPathTree, inspectNode);
            return truncPathTree;
        }
        //第二轮,只比最底层元素: 加上has or not has empty or not empty or nth-child   
        //目前前面展示并不支持 has/not has等，故这一步不走，若以后开启，记得将stepOneWithAddPathProp的叶子nth-child逻辑处理去除 
        //stepTwoWithAddPathProp();   
        return truncPathTree;
        //has > not has > empty > not empty > nth-child   
        //prop: id > name > class     
        //需要和所有的子节点比
        function stepTwoWithAddPathProp() {
            var diffElementList = findTreeLevelElements(targetElements, 1);
            if (diffElementList.length > 0) {
                //console.info(\"error:到这一步，原则上应该是都是同一parent才对\");          
                return false;
            }
            //get prop
            var nodeArray = IIRPA.dom.nodePropArray(inspectNode);
            //diff         
            var needCheck = false;
            for (var j = 0; j < targetElements.length; j++) {
                var tmpElement = targetElements[j];
                if (tmpElement == inspectNode)
                    continue;
                for (var k = 0; k < nodeArray.length; k++) {
                    var propName = nodeArray[k].name;
                    var hasValue = nodeArray[k].has || false;
                    var emptyValue = nodeArray[k].empty || true;
                    var hasSame = nodeArray[k].hasSame || false;
                    if (hasSame)
                        continue;
                    //check has               
                    var siblingHasAttribute = IIRPA.dom.nodeHasAttribute(tmpElement, propName);
                    if (hasValue && siblingHasAttribute) {
                        nodeArray[k].hasSame = true;
                        continue;
                    }
                    //check has not               
                    if (!hasValue && !siblingHasAttribute) {
                        nodeArray[k].hasSame = true;
                        continue;
                    }
                    //has empty              
                    if (hasValue && siblingHasAttribute) {
                        //empty                
                        var siblingAttrValue = tmpElement.getAttribute(propName);
                        if (emptyValue && !siblingAttrValue) {
                            nodeArray[k].hasSame = true;
                            continue;
                        }
                        //not empty               
                        if (!emptyValue && siblingAttrValue) {
                            nodeArray[k].hasSame = true;
                            continue;
                        }
                    }
                    needCheck = true;
                }
                if (!needCheck) {
                    break;
                }
            }
            //有区分，has > not has > empty > not empty
            var leafPath = truncPathTree[truncPathTree.length - 1];
            if (needCheck) {
                var diffProp = findDiffProp(nodeArray);
                if (diffProp) {
                    var prop = leafPath.props;
                    prop.push(diffProp);
                    return true;
                }
            }
            //必须加nth-child todo
            var optionPropArray = leafPath.optionProps || [];
            //属性是否存在,if exists -> get value        
            var nthChild = getPropValueFromOptionArray(optionPropArray, IIRPA.dom.Attr.NTH_CHILD);
            if (nthChild) {
                var prop = leafPath.props;
                prop.push(IIRPA.buildProp(IIRPA.dom.Attr.NTH_CHILD, nthChild, IIRPA.PropPattern.EQUAL, true));
                return true;
            }
        }
        //从diffArray中找出必要的属性
        //has > not has > empty > not empty    
        function findDiffProp(diffArray) {
            for (var i = 0; i < 4; i++) {
                for (var j = 0; j < diffArray.length; j++) {
                    if (diffArray[j].hasSame) continue;
                    switch (i) {
                        //loop 1:has                
                        case 0:
                            if (diffArray[j].has) {
                                return IIRPA.buildProp(diffArray[j].name, '', IIRPA.PropPattern.HAS, true);
                            }
                            break;
                        //loop 2 :not has             
                        case 1:
                            if (!diffArray[j].has) {
                                return IIRPA.buildProp(diffArray[j].name, '', IIRPA.PropPattern.NOT_HAS, true);
                            }
                            break;
                        //loop 3: empty               
                        case 2:
                            if (diffArray[j].empty) {
                                return IIRPA.buildProp(diffArray[j].name, '', IIRPA.PropPattern.EMPTY, true);
                            }
                            break;
                        //loop 4: not empty               
                        case 3:
                            if (!diffArray[j].empty) {
                                return IIRPA.buildProp(diffArray[j].name, '', IIRPA.PropPattern.NOT_EMPTY, true);
                            }
                            break;
                    }
                }
            }
        }
        function stepOneWithAddPathProp() {
            //id > text(最低层) > class > nth-child(非叶子)  
            //但最后仍然不行，nth-child需要再加     
            var filterPropArray = IIRPA.locale.filterPropArray();
            var treeLength = truncPathTree.length;
            for (var k = 0; k < filterPropArray.length; k++) {
                var filterProp = filterPropArray[k];
                var toBeAddedParamName = filterProp.name;
                //两种位置有关的属性             
                var isOnlyUsingOnLeaf = filterProp.onLeaf;
                var isOnlyUsingOnParent = filterProp.onParent;
                for (var i = treeLength - 1; i >= 0; i--) {
                    //check using on leaf      
                    if (isOnlyUsingOnLeaf && i != treeLength - 1) {
                        break;
                    }
                    //check using on parent               
                    if (isOnlyUsingOnParent && i == treeLength - 1) {
                        continue;
                    }
                    var diffResult = diffProp(targetElements, i, toBeAddedParamName);
                    if (!diffResult.result) {
                        continue;
                    }
                    targetElements = diffResult.targetElements;
                    if (targetElements.length == 1) {
                        return true;
                    }
                }
            }
            //最后的最后：叶子的nth-child
            var diffResult = diffProp(targetElements, treeLength - 1, IIRPA.dom.Attr.NTH_CHILD);
            if (diffResult.result) {
                targetElements = diffResult.targetElements;
                if (targetElements.length == 1) {
                    return true;
                }
            }
            return false;
        }
        /**      * /注意：pathTree中的顺序是根->叶， 但在算树层时，是从叶->根      * @param {*} targetElements 待对比的元素集合      * @param {*} pathTreeIndex truncPathTree中的次序      * @param {*} toBeAddedParamName diff属性      */
        function diffProp(targetElements, pathTreeIndex, toBeAddedParamName) {
            var i = pathTreeIndex;
            var pathNode = truncPathTree[i];
            var optionPropArray = truncPathTree[i].props || [];
            //属性是否存在,if exists -> get value       
            var toBeAddedParamValue = getPropValueFromOptionArray(optionPropArray, toBeAddedParamName);
            if (!toBeAddedParamValue) {
                return { result: false };
            }
            //already added        
            var propArray = pathNode.props || [];
            if (isAddedParam(propArray, toBeAddedParamName)) {
                return { result: false };
            }
            //针对某些特别属性处理
            var propValuePattern = IIRPA.PropPattern.EQUAL;
            var treeLevel = truncPathTree.length - 1 - i;
            switch (toBeAddedParamName.toLocaleLowerCase()) {
                //id需要不含有数字           
                case IIRPA.dom.Attr.ID:
                    if (!IIRPA.dom.usefulIdValue(toBeAddedParamValue)) {
                        return { result: false };
                    }
                    break;
                case IIRPA.dom.Attr.CLASS:
                    //去除同层中都有的class,            
                    var filterClass = diffDistinctClass(targetElements, treeLevel, toBeAddedParamValue);
                    if (!filterClass) {
                        return { result: false };
                    }
                    toBeAddedParamValue = filterClass;
                    //reAssign             
                    propValuePattern = IIRPA.PropPattern.CONTAIN;
                    break;
                case IIRPA.dom.Attr.NTH_CHILD:
                    //如果同层中都是一个值,就不加了             
                    if (!canUsingNthChild(targetElements, treeLevel, toBeAddedParamValue)) {
                        return { result: false };
                    }
                    break;
            }
            //组织truncPathTree
            var props = truncPathTree[i].props || [];
            for (var i = 0; i < props.length; i++) {
                if (props[i].name === toBeAddedParamName && props[i].value === toBeAddedParamValue) {
                    props[i].accurate = "true";
                }
            }
            //props.push(IIRPA.buildProp(toBeAddedParamName, toBeAddedParamValue, propValuePattern, true));
            //check tree      
            targetElements = IIRPA.locale.findElements(truncPathTree);
            if (!targetElements || targetElements.length == 0) {
                //若发生，说明findElements有bug或页面发了变化      
                //console.info(\"发生了异常情况：根据truncPathTree未找到元素\");       
            }
            return { result: true, targetElements: targetElements };
        }
        //对比出不同的nth-child
        //只要diff出来一个不同的，就可以使用   
        //这里若有性能问题，可以在findTreeLevelElements去掉，因为只要找到不同，不需要取所有同层     
        function canUsingNthChild(elementsList, treeLevel, nthChild) {
            IIRPA.trace("check nthChild:" + nthChild);
            var diffElementList = findTreeLevelElements(elementsList, treeLevel);
            for (var j = 0; j < diffElementList.length; j++) {
                var element = diffElementList[j];
                var eleNthChild = IIRPA.dom.getNthChild(element);
                IIRPA.trace("level element's nthChild:" + eleNthChild);
                if (nthChild != eleNthChild) {
                    return true;
                }
            }
            return false;
        }
        //比对出不同的class出来
        //注意,不同elementsList的某个treeLevel会汇聚在一个parent上
        //注意，排除自己     
        function diffDistinctClass(elementsList, treeLevel, classValue) {
            IIRPA.trace("check class:" + classValue);
            var diffElementList = findTreeLevelElements(elementsList, treeLevel);
            //将classValue中的值,分成多个        
            var classArray = IIRPA.dom.convertClassToArray(classValue);
            for (var j = 0; j < diffElementList.length; j++) {
                var element = diffElementList[j];
                var eleClassValue = element.getAttribute('class');
                IIRPA.trace("level element's class:" + eleClassValue);
                if (!eleClassValue)
                    continue;
                //if classArray中含有class在element中，从classArray中移除      
                removeSameClass();
                if (classArray.length == 0)
                    return '';
            }
            return classArray && classArray.length > 0 ? classArray.join(" ") : '';
            //移除classArray中eleClassValue中存在的class        
            function removeSameClass() {
                for (var j = classArray.length - 1; j >= 0; j--) {
                    var regStr = "\\b" + classArray[j] + "\\b";
                    if (IIRPA.regMatch(eleClassValue, regStr)) {
                        classArray.splice(j, 1);
                    }
                }
            }
        }
        /*
         * //检查 看实现这里的treeLevel应该是从下->上来找的
         * 但看前面参数的传递，感觉这里的treeLevel就从上往下的，要调一下 好像又是对的。。。 
         * 找出childElementList集体内,某一父层的元素列表
         * 注意排除inspectNode所在分支上的元素，否则diff没有意义
         * @param {*} childElementList 
         * @param {*} treeLevel
         */
        function findTreeLevelElements(childElementList, treeLevel) {
            var prevElement = inspectNode;
            var prevTreeLevelElements = childElementList;
            for (var i = 0; i <= treeLevel; i++) {
                var treeLevelElements = [];
                for (var j = 0; j < prevTreeLevelElements.length; j++) {
                    var loopEle;
                    if (i == 0)
                        loopEle = prevTreeLevelElements[j];
                    else
                        loopEle = prevTreeLevelElements[j].parentNode;
                    if (loopEle != prevElement && IIRPA.indexOfArray(treeLevelElements, loopEle) < 0) {
                        treeLevelElements.push(loopEle);
                    }
                }
                prevTreeLevelElements = treeLevelElements;
                prevElement = prevElement.parentNode;
            }
            return prevTreeLevelElements;
        }
        //get paramName.value from optionPropArray   
        function getPropValueFromOptionArray(optionPropArray, paramName) {
            for (var j = 0; j < optionPropArray.length; j++) {
                var propObj = optionPropArray[j];
                if (propObj.name.toLowerCase() == paramName.toLowerCase() && propObj.value) {
                    return propObj.value;
                }
            }
            return '';
        }
        //paramName是否已在propArray中  
        function isAddedParam(propArray, paramName) {
            for (var j = 0; j < propArray.length; j++) {
                var propObj = propArray[j];
                if (propObj.name.toLowerCase() == paramName.toLowerCase() && propObj.accurate === "true") {
                    return true;
                }
            } return false;
        }
    }
    /**  * 找根据pathTree找元素  *   核心是转换成css-selector来快速定位,但存在一些属性的特别匹配方式，需要再回头过滤  * @param {*} pathTree   * @param {*} parent   */
    IIRPA.locale.findElements = function (pathTree, parent, version, discernHidden) {
        parent = parent || document;
        version = parseFloat(version || "1.0");
        if (!pathTree || !IIRPA.isArray(pathTree)) {
            throw new IIRPA.Error(IIRPA.ErrorCode.INVALID_PARAM, "pathTree(" + pathTree + ") is invalid.");
        }
        var resultElements = [];
        //result elements     
        //第一步，转换pathTree->trunc css path同时按树层次，将没有校验的属性记录下来    
        var truncCssPath;
        var notVerifyPropsOfTree = [];
        truncCssPath = convertPathTreeToTruncPath();
        IIRPA.trace("findElements truncCssPath:" + truncCssPath)
        //第二步:获取truncCssPath下的元素    
        var elements = IIRPA.dom.querySelectorAll(truncCssPath, parent);
        //not found   
        if (!elements || elements.length == 0) {
            return resultElements;
        }
        IIRPA.trace("findElements elementsFromCssPath:" + elements.length);
        //no notVerifyPropsOfTree     
        if (notVerifyPropsOfTree.length == 0) {
            //转成数组类型        
            for (var i = 0; i < elements.length; i++) {
                if (discernHidden !== false || !IIRPA.dom.IsElementHidden(elements[i], true)) {
                    resultElements.push(elements[i])
                }
            }
            return resultElements;
        }
        IIRPA.trace("findElements notVerifyPropsOfTree:" + IIRPA.JSON().stringify(notVerifyPropsOfTree));
        //need filter,one by one    
        for (var i = 0; i < elements.length; i++) {
            var element = elements[i];
            if (checkElementContentProps(element, notVerifyPropsOfTree) && (discernHidden !== false || !IIRPA.dom.IsElementHidden(elements[i], true))) {
                resultElements.push(element);
            }
        }
        return resultElements;
        //pathTree->trunc css path    
        function convertPathTreeToTruncPath() {
            var cssPathSteps = [];
            var treeLength = IIRPA.dom.getValidPathLength(pathTree);
            for (var j = 0; j < treeLength; j++) {
                var treeNodeParam = pathTree[j];
                var nodeName = IIRPA.cssPathEscape(treeNodeParam.name);
                var props = treeNodeParam.props;
                var step = '';
                //处理属性           
                if (props && props.length > 0) {
                    //属性先按id > class > name > ... >text排序 todo       
                    //除text外,其它均可拼接到css                
                    //pattern中,equal与has可拼接到css中
                    //对于class比较特殊，只有class=contain时才能加到css中     
                    var notVerfityPropsOfNode = [];
                    for (var k = 0; k < props.length; k++) {
                        if (!(props[k].accurate === "true"))
                            continue
                        var pattern = props[k].pattern;
                        var name = props[k].name;
                        if (!name) {
                            continue;
                        }

                        if (addToSelector(name, pattern)) {
                            step += buildPropCssSelector(name, props[k].value, pattern, version);
                            continue;
                        }
                        if (version > 1 && treeNodeParam.accurate != 'true') {
                            continue;
                        }
                        //其它需要二次过滤                    
                        notVerfityPropsOfNode.push(props[k]);
                    }
                    //add to notVerfityPropsOfNode
                    if (notVerfityPropsOfNode.length > 0) {
                        notVerifyPropsOfTree.push({ level: treeLength - j - 1, props: notVerfityPropsOfNode });
                    }
                }
                step = nodeName + step;
                cssPathSteps.push(step);
            }
            if (version > 1) {
                var reduce = function (list, callback, initialValue) {
                    var previous = initialValue, k = 0, length = list.length;
                    for (k; k < length; k++) {
                        list.hasOwnProperty(k) && (previous = callback(previous, list[k], k, list));
                    }
                    return previous;
                };
                return reduce(cssPathSteps, function (result, item, index) {
                    var current = pathTree[index].accurate === 'true';
                    if (!current) {
                        return result + ' ';
                    }
                    if (index === 0) {
                        return item;
                    } else {
                        var previous = pathTree[index - 1].accurate === 'true';
                        return result + (previous ? '>' : '') + item;
                    }
                }, '');
            }
            return cssPathSteps.join('>')
        }
        /**      * 此属性是否可加入到cssSelector中      * @param {*} name       * @param {*} value       * @param {*} pattern       */
        function addToSelector(name, pattern) {
            switch (pattern.toLowerCase()) {
                case IIRPA.PropPattern.EQUAL:
                    return name != IIRPA.dom.Attr.TEXT && name != IIRPA.dom.Attr.CLASS && name != IIRPA.dom.Attr.SRC && name != IIRPA.dom.Attr.OUTERTEXT && name != IIRPA.dom.Attr.ELEVISIBLE;
                case IIRPA.PropPattern.HAS:
                case IIRPA.PropPattern.EMPTY:
                case IIRPA.PropPattern.CONTAIN:
                    return name != IIRPA.dom.Attr.SRC && name != IIRPA.dom.Attr.TEXT && name != IIRPA.dom.Attr.OUTERTEXT;
                case IIRPA.PropPattern.EXCLUDE:
                    return name == IIRPA.dom.Attr.CLASS;
            }         return false;
        }
        /**      * 比如 #id .class [attribute=]      * @param {*} name       * @param {*} value       * @param {*} pattern       */
        function buildPropCssSelector(name, value, pattern, version) {
            switch (pattern.toLowerCase()) {
                case IIRPA.PropPattern.EQUAL:
                    if (!value) {
                        //console.info("prop.value is empty,name=" + name);   
                        return '';
                    }
                    switch (name.toLowerCase()) {
                        case IIRPA.dom.Attr.NTH_CHILD:
                            return ':nth-child(' + value + ')';
                        case "nth-child":
                            return ':nth-child(' + value + ')';
                        case IIRPA.dom.Attr.ID:
                            return '#' + IIRPA.escapeIdentifierIfNeeded(value);
                        default:
                            return '[' + name + '=\"' + value + '\"]';
                    }
                    break;
                case IIRPA.PropPattern.CONTAIN:
                    if (name.toLowerCase() === IIRPA.dom.Attr.CLASS && version > 1) {
                        var map = function (list, fn, context) {
                            var arr = [];
                            for (var k = 0, length = list.length; k < length; k++) {
                                arr.push(fn.call(context, list[k], k, list));
                            }
                            return arr;
                        };
                        return map(value.split(' '), function (c) { return c ? '.' + IIRPA.escapeIdentifierIfNeeded(c) : ''; }).join('');
                    }
                    return '[' + name + '*="' + value + '"]';
                //if (name.toLowerCase() === IIRPA.dom.Attr.CLASS) {
                //    //空格替换成.                  
                //    return '.' + IIRPA.trim(value).replace(/\s+/g, '.');
                //}
                //else {
                //   return '[' + name + '*="' + value + '"]';
                //}
                case IIRPA.PropPattern.HAS:
                    return '[' + name + ']';
                case IIRPA.PropPattern.EMPTY:
                    return '[' + name + '=""]';
                case IIRPA.PropPattern.EXCLUDE:
                    if (name.toLowerCase() == IIRPA.dom.Attr.CLASS) {
                        return ':not(' + _classToSelector(value) + ')';
                    }
            }
        }
        /**      * 将class以空格分开的值，拼接成class      * @param {string} classValue       */
        function _classToSelector(classValue) {
            var array = IIRPA.dom.convertClassToArray(classValue);
            var selector = '';
            for (var i = 0; i < array.length; i++) {
                selector += "." + IIRPA.escapeIdentifierIfNeeded(array[i]);
            }
            return selector;
        }
        /**      * 校验属性是否符合属性树propTree上的属性      * @param {*} element       * @param {*} propTree       */
        function checkElementContentProps(element, propTree) {
            //获取最大的level,根据前面可知propTree的顺序的是level从大到小,from 0      
            var maxLevel = propTree[0].level;
            var elementTree = IIRPA.dom.serachTree(element, maxLevel);
            for (var j = 0; j < propTree.length; j++) {
                var level = propTree[j].level;
                var node = elementTree[level];
                var props = propTree[j].props;
                IIRPA.trace('level:' + level);
                IIRPA.trace('props:' + IIRPA.JSON().stringify(props));
                for (var i = 0; i < props.length; i++) {
                    var pattern = props[i].pattern || IIRPA.PropPattern.EQUAL;
                    var name = props[i].name.toLowerCase();
                    var value = props[i].value;
                    var nodeAttrValue = IIRPA.dom.attrValue(node, name);
                    IIRPA.trace('diff:name=' + name + 'propValue=' + value + ',nodeAttrValue=' + nodeAttrValue);
                    //按pattern类型处理                
                    switch (pattern.toLowerCase()) {
                        case IIRPA.PropPattern.EQUAL:
                            if (!checkEqual(name, value, nodeAttrValue)) {
                                return false;
                            }
                            break;
                        case IIRPA.PropPattern.CONTAIN:
                            if (!checkContain(name, value, nodeAttrValue)) {
                                return false;
                            }
                            break;
                        case IIRPA.PropPattern.REGULAR:
                            if (!IIRPA.regMatch(nodeAttrValue, value)) {
                                return false;
                            }
                            break;
                        case IIRPA.PropPattern.BEGIN:
                            if (!IIRPA.regMatch(nodeAttrValue, "^" + value)) {
                                return false;
                            }
                            break;
                        case IIRPA.PropPattern.END:
                            if (!IIRPA.regMatch(nodeAttrValue, value + "$")) {
                                return false;
                            }
                            break;
                        case IIRPA.PropPattern.HAS:
                            if (!IIRPA.dom.nodeHasAttribute(node, props[i].name)) {
                                return false;
                            }
                            break;
                        case IIRPA.PropPattern.NOT_HAS:
                            if (IIRPA.dom.nodeHasAttribute(node, props[i].name)) {
                                return false;
                            }
                            break;
                        case IIRPA.PropPattern.EMPTY:
                            if (!IIRPA.dom.nodeHasAttribute(node, props[i].name) || nodeAttrValue) {
                                return false;
                            }
                            break;
                        case IIRPA.PropPattern.NOT_EMPTY:
                            if (!nodeAttrValue) {
                                return false;
                            }
                            break;
                        default:
                    }
                }
            }
            return true;
            //! 针对布尔值属性要不区分大小写   
            //! 针对text,去除两边空格再对比    
            function checkEqual(propName, propValue, nodeValue) {
                if (propName == IIRPA.dom.Attr.TEXT) {
                    propValue = IIRPA.trim(propValue);
                    nodeValue = IIRPA.trim(nodeValue);
                }
                IIRPA.trace('diff equal:propValue=' + propValue + ',nodeValue=' + nodeValue);
                return propValue == nodeValue;
            }
            //propValue是否包含在nodeValue中     
            //若是class,需要取每一个class 对比每一个class  
            function checkContain(propName, propValue, nodeValue) {
                if (propName == IIRPA.dom.Attr.CLASS) {
                    var propClassArray = IIRPA.dom.convertClassToArray(propValue);
                    var nodeValueClassArray = IIRPA.dom.convertClassToArray(nodeValue);
                    //校验propClassArray中的每一项是否在nodeValueClassArray中          
                    for (var j = 0; j < propClassArray.length; j++) {
                        if (propClassArray[j] && IIRPA.indexOfArray(nodeValueClassArray, propClassArray[j]) < 0) {
                            return false;
                        }
                        //包含了第一个class 属性 先return
                        // return true;
                    }
                    return true;
                }
                IIRPA.trace('diff contain:propValue=' + propValue + ',nodeValue=' + nodeValue);
                return nodeValue.indexOf(propValue) > -1;
            }
        }
    }
    /**  * 找表格，只在当前文档中找，非嵌套iframe内  * @param {string} matchType   * @param {string} matchValue   * @param {string} returnType   */
    IIRPA.locale.findTable = function (matchType, matchValue, returnType, parent) {
        var tables = IIRPA.dom.tables(parent);
        var matchTable;
        switch (matchType) {
            case 'index':
                if (matchValue <= tables.length) {
                    matchTable = tables[matchValue - 1]
                }
                break;
            case 'text':
                matchTable = matchFromText(tables, matchValue);
                break;
            case 'html':
                matchTable = matchFromHtml(tables, matchValue);
                break;
            default:
                throw new IIRPA.Error(IIRPA.ErrorCode.INVALID_PARAM, "未知的配置类型(" + matchType + ")");
        }
        if (!matchTable) {
            throw new IIRPA.Error(IIRPA.ErrorCode.INVALID_PARAM, "未匹配到目标表格，请检查参数");
        }
        return IIRPA.dom.tableData(matchTable, returnType);
        function matchFromText(tables, text) {
            for (var i = 0; i < tables.length; i++) {
                if (IIRPA.regMatch(tables[i].outerText, text)) {
                    return tables[i];
                }
            }
        }
        function matchFromHtml(tables, text) {
            for (var i = 0; i < tables.length; i++) {
                if (IIRPA.regMatch(tables[i].outerHTML, text)) {
                    return tables[i];
                }
            }
        }
    }
    /**  * 根据所有元素找一个共同的路径  */
    IIRPA.locale.similarPath = function (elements, newElement) {
        //对比这些元素的路径，并去除不同的部分，得到全路径   
        var resultPathArray = diffAndRemoveDiffenceNthChild(elements, newElement);
        //优化resultPathArray,将含有nth-child的元素替换成其它信息，尽量不用nth-child    
        var selector = resultPathArray.join(">");
        var nodes = IIRPA.dom.querySelectorAll(selector);
        var pathTree = [];
        var levelNodes = nodes;
        var findUniqueId = false;
        for (var i = resultPathArray.length; i >= 0; i--) {
            var pathItem = resultPathArray[i];
            var nodeName = getNodeName(pathItem);
            var optionProps = [];
            var params = [];
            //find levelNodes
            levelNodes = levelNodes(i, levelNodes);
            if (levelNodes == 1) {
                //有无唯一id             
                if (IIRPA.dom.usefulId(levelNodes[0]) && IIRPA.dom.checkIdUnique(nodeName, levelNodes[0].id, IIRPA.dom.getRootNode(levelNodes[0]))) {
                    findUniqueId = true;
                    params.push(IIRPA.buildProp(IIRPA.dom.Attr.ID, levelNodes[0].id, IIRPA.PropPattern.EQUAL, true));
                }
                optionProps = IIRPA.dom.getNodeOptionProps(levelNodes[0], true);
            }
            pathTree.push(IIRPA.buildPath(nodeName, params, optionProps));
            //find and break;        
            if (findUniqueId) {
                break;
            }
        }
        return pathTree.reverse();
        function levelNodes(level, nextLevelNodes) {
            if (level == 0)
                return nextLevelNodes;
            //find nextLevelNodes.parent       
            var nodes = [];
            for (var i = 0; i < nextLevelNodes.length; i++) {
                var node = nextLevelNodes[i].parentNode;
                if (IIRPA.indexOfArray(nodes, node) < 0)
                    nodes.push(node);
            }
            return nodes;
        }
        function diffAndRemoveDiffenceNthChild(elements, newElement) {
            //找全路径      
            var newPath = IIRPA.dom.cssAllPath(newElement);
            //find all elements path and check similar        
            var elementPathArray = [];
            for (var i = 0; i < elements.length; i++) {
                var path = IIRPA.dom.cssAllPath(elements[i]);
                //check是否为相似元素,这里看一下是否只校验长度        
                if (path.length != newPath.length)
                    throw new IIRPA.Error(IIRPA.ErrorCode.Failure, "不是相似元素");
                elementPathArray.push(path);
            }
            //diff    
            var resultPathArray = [];
            for (var i = 0; i < newPath.length; i++) {
                var pathItem = newPath[i];
                var nodeName = getNodeName(pathItem);
                var nthChild = getNthChild(pathItem);
                //check from elements       
                var sameNthChild = true;
                for (var j = 0; j < elementPathArray.length; j++) {
                    var path = elementPathArray[j];
                    var elementPathItem = path[i];
                    var elementNodeName = getNodeName(elementPathItem);
                    var elementNthChild = getNthChild(elementPathItem);
                    if (nodeName != elementNodeName) {
                        throw new IIRPA.Error(IIRPA.ErrorCode.Failure, "不是相似元素");
                    }
                    if (nthChild != elementNthChild)
                        sameNthChild = false;
                }
                //if sameNthChild,保留，否则去除             
                var resultPathItem = nodeName;
                if (sameNthChild) {
                    resultPathItem += ":nth-child(" + nthChild + ")";
                }
                resultPathArray.push(resultPathItem);
            }
            return resultPathArray;
        }
        function getNodeName(pathItem) {
            var end = pathItem.indexOf(":") == -1 ? pathItem.length : pathItem.indexOf(":");
            return pathItem.substr(0, end);
        }
        function getNthChild(pathItem) {
            var start = pathItem.indexOf(":") == -1 ? pathItem.length : pathItem.indexOf(":");
            if (start == pathItem.length)
                return "";
            else
                return pathItem.substr(start + 1);
        }
        /**      * 长度是否相同，是否有相同的父亲      * @param {*} path1       * @param {*} path2       */
        function isSimilarPath(pathArray1, pathArray2) {
            if (pathArray1.length != pathArray2.length)
                return false;
            //路径上的nodeName必须是一样的，nth-child可以不一样     
            for (var i = 0; i < pathArray1.length; i++) {
                var path1 = pathArray1[i];
                var end1 = path1.indexOf(":") == -1 ? path1.length : path1.indexOf(":");
                var nodeName1 = path1.substr(0, end)
                var path2 = pathArray2[i];
                var end2 = path2.indexOf(":") == -1 ? path2.length :
                    path2.indexOf(":");
                var nodeName2 = path2.substr(0, end)
                if (nodeName1 != nodeName2)
                    return false;
            }
            return true;
        }
    }

    IIRPA.dom.getValidPathLength = function (pathTree) {
        var treeLength = 0;
        for (var i = pathTree.length; i > 0; i--) {
            if (pathTree[i - 1].accurate === 'true') {
                treeLength = i;
                break;
            }
        }
        return treeLength;
    }
    IIRPA.dom.getTableNodeName = function (node) {
        var contextNode = node;
        var isTable = false;
        while (contextNode && contextNode.parentNode) {
            var nodeName = IIRPA.dom.lowerCaseNodeName(contextNode);
            if (nodeName == 'body' || nodeName == 'html')
                break;
            if (nodeName == "table") {
                isTable = true;
                break;
            }
            contextNode = contextNode.parentNode;
        }
        var result = { "isTable": isTable, "element": contextNode }
        return { 'status': IIRPA.ErrorCode.SUCCESS, 'value': result };
    }
    IIRPA.api.scrollIntoViewIfNeededorscrollIntoView = function (ele) {
        if (window.mozInnerScreenX !== undefined)
            ele.scrollIntoView({ behavior: "auto", block: "center", inline: "center" });
        else
            ele.scrollIntoViewIfNeeded() //默认参数为True，让元素在可视区域中居中对齐
    }
    IIRPA.api.getNextElement = function (cssSelector) {
        if (cssSelector) {
            var ele = IIRPA.dom.querySelector(cssSelector);
            if (ele) return ele;
        }
        var pageTexts = [/^下一页/, /^next/]
        //const unQualifyTags = ['script', 'header', 'head', 'html', 'meta', 'style', 'title', 'body']
        var elements = IIRPA.dom.querySelectorAll("*");
        for (var i = 0; i < elements.length; i++) {
            var ele = elements[i];
            for (var j = 0; j < pageTexts.length; j++) {
                if (ele.innerHTML.replace(/\s+/g, '').match(pageTexts[j])) {
                    return ele
                }
            }
        }
    }
    IIRPA.api.getIsTableName = function (nodeIdentify) {
        var retval = {};
        try {
            if (nodeIdentify) {
                var result = IIRPA.dom.getTableNodeName(nodeIdentify);
                return { 'status': IIRPA.ErrorCode.SUCCESS, 'value': result.value };
            }
        } catch (e) {
            retval = { 'status': e.code || IIRPA.ErrorCode.ENTITY_REFERENCE, 'value': false };
        }
        return retval || {};
    }
    IIRPA.api.isExistsAttributes = function (attrs, attrName) {
        var retval = {};
        try {
            for (var i = 0; i < attrs.length; i++) {
                if (attrs[i] && attrs[i].toLowerCase().indexOf(attrName) > -1)
                    return true
            }
        } catch (e) {
            return false
        }
        return false
    }
    IIRPA.api.getAttributes = function (nodeIdentify) {
        var retval = {};
        try {
            var result = [];
            result = IIRPA.dom.nodeAttributeName(nodeIdentify);
            result.push("innerText");
            return { 'status': IIRPA.ErrorCode.SUCCESS, 'value': IIRPA.JSON().stringify(result) };
        } catch (e) {
            retval = { 'status': e.code || IIRPA.ErrorCode.ENTITY_REFERENCE, 'value': e.message };
        }
        return retval || {};
    }
    IIRPA.api.getAttributeValue = function (ele, attrName, isIntact) {
        var attrVal = null;
        var isExistAttr = IIRPA.api.hasAttribute(ele, attrName);
        if (!isExistAttr) {
            attrVal = null;
        } else if (attrName == "src") {
            attrVal = ele.src || ele.getAttribute("src") || "";
        } else if (attrName == "href") {
            attrVal = ele.href || ele.getAttribute("href") || "";
        } else if (attrName.toLowerCase() == "innertext") {
            attrVal = (ele.innerText || ele.outerText) || "";
        } else if (attrName == "absoluteurl") {
            if (isIntact) {
                attrVal = ele.href || ele.src || "";
            }
            else {
                attrVal = ele.getAttribute("href") || ele.getAttribute("src") || "";
            }
        }
        else {
            var domAttrName = IIRPA.SpecialAttributes[attrName.toUpperCase()] || attrName;
            var domAttrValue = ele[domAttrName];
            if (domAttrValue !== undefined && domAttrValue !== null) {
                attrVal = domAttrValue.toString(); // 确保返回字符串
            } else {
                // 如果 DOM 属性不存在，则通过 getAttribute 获取 HTML 属性值
                attrVal = ele.getAttribute(domAttrName) || "";
            }
        }
        return { 'value': attrVal };
    }
    IIRPA.api.hasAttribute = function (ele, attrName) {
        if (attrName.toLowerCase() == "innertext") {
            return true;
        }
        var isExistAttr;
        if (ele.hasAttribute) {
            isExistAttr = ele.hasAttribute(attrName);
        } else {
            isExistAttr = ele.getAttribute(attrName) !== null || ele.attributes[attrName] !== undefined;
        }
        if (isExistAttr) {
            return true;
        }
        if (attrName == "absoluteurl") {
            return IIRPA.api.hasAttribute(ele, "href") || IIRPA.api.hasAttribute(ele, "src");
        }
        if (ele[attrName] !== undefined && ele[attrName] !== "") {
            return true;
        }
        return false;
    }
    IIRPA.api.inspectcssSelector = function (nodeIdentify) {
        var retval = {};
        try {
            var path = '';
            var text = '';
            var node = IIRPA.locale.findNode(nodeIdentify);
            if (node) {
                path = IIRPA.dom.cssAllPath(node).reverse().join(">")
            }
            var t = node.innerText || node.outerText
            var length = IIRPA.trim(t).length;
            if (t && length < 20) {
                text = t
            }
            var result = {
                "path": path || "",
                "innertext": text || ""
            };
            return { 'status': IIRPA.ErrorCode.SUCCESS, 'value': result };
        } catch (e) {
            retval = { 'status': e.code || IIRPA.ErrorCode.ENTITY_REFERENCE, 'value': e.message };
        }
        return retval || {};
    }
    //API能力
    IIRPA.api.injectStatus = function () {
        window._iirpa_injectCodeID = '$@{ExtensionProvider_CODE_ID}';
        window._iirpa_injectstatus = "success";
    }
    IIRPA.api.findBySelector = function (selector, index, parentIdentify) {
        index = index || 1;
        var root;
        try {
            if (parentIdentify) {
                root = IIRPA.locale.findNode(parentIdentify)
                if (!root) {
                    return { 'status': IIRPA.ErrorCode.NO_SUCH_ELEMENT, 'value': '未找到父控件' };
                }
            }
            var elements = IIRPA.dom.querySelectorAll(selector, root);
            if (elements && elements.length >= index) {
                return { 'status': IIRPA.ErrorCode.SUCCESS, 'value': elements[index - 1] };
            }
            return { 'status': IIRPA.ErrorCode.NO_SUCH_ELEMENT, 'value': '未找到控件' };
        } catch (e) {
            return { 'status': IIRPA.ErrorCode.INVALID_PARAM, 'value': e.message };
        }
    }
    IIRPA.api.inspectxPath = function (nodeIdentify) {
        function readxPath(node) {
            if (node.id) {
                //显 示//*[@id="xPath"]  形式内容
                return '//*[@id=\"' + node.id + '\"]';
            }
            if (node == document.body) {
                return '/html/' + node.tagName.toLowerCase();
            }
            var ix = 1;//每次点击初始化
            var siblings = node.parentNode.childNodes;//同级的子元素

            for (var i = 0, l = siblings.length; i < l; i++) {
                var sibling = siblings[i];
                if (sibling == node) {
                    //递归
                    return arguments.callee(node.parentNode) + '/' + node.tagName.toLowerCase() + '[' + (ix) + ']';
                }
                else if (sibling.nodeType == 1 && sibling.tagName == node.tagName) {
                    ix++;
                }
            }
        }

        var retval = {};
        try {
            var path = ""
            var node = IIRPA.locale.findNode(nodeIdentify);
            if (node) {
                path = readxPath(node)
            }
            return { 'status': IIRPA.ErrorCode.SUCCESS, 'value': path };
        } catch (e) {
            retval = { 'status': e.code || IIRPA.ErrorCode.ENTITY_REFERENCE, 'value': e.message };
        }
        return retval || {};
    }

    //传入元素的uniqueID,计算元素的唯一路径
    //若是iframe的元素会传入多次
    IIRPA.api.inspectPath = function (nodeIdentify, options) {
        options = options || {};
        function filterPropertyValue(props) {
            for (var i = props.length - 1; i >= 0; i--) {
                var propVal = props[i].value;
                if (!IIRPA.isNumber(propVal) && !IIRPA.isString(propVal)) {
                    props.splice(i, 1);
                }
            }
        }
        var retval = {};
        try {
            var node = IIRPA.locale.findNode(nodeIdentify);
            //find from node      
            //var path = IIRPA.locale.validPathFromNode(node);
            //if (!path) {
            //    path = IIRPA.locale.extractUniquePath(node, options);
            //}
            var path = IIRPA.locale.validPathFromNode(node);
            if (path) {

                var truncPathTree = IIRPA.dom.truncPathTree(node.parentNode, true);
                var path_anchorAncestor = IIRPA.locale.extractUniquePathByIgnoreAnchor(node.parentNode, truncPathTree.pathTree, options);
                path = path_anchorAncestor.concat(path);
            }
            else {
                var truncPathTree = IIRPA.dom.truncPathTree(node, true);
                if (truncPathTree.firstUniqueEleIdx === -1) {
                    path = IIRPA.locale.extractUniquePath(node, truncPathTree.pathTree, options)
                }
                else {
                    var truncPathTree_anchorAncestor = truncPathTree.pathTree.slice(0, truncPathTree.firstUniqueEleIdx);
                    var truncPathTree_anchorToEle = truncPathTree.pathTree.slice(truncPathTree.firstUniqueEleIdx);
                    var path_anchorAncestor = [];
                    if (truncPathTree_anchorAncestor.length > 0) {
                        path_anchorAncestor = IIRPA.locale.extractUniquePathByIgnoreAnchor(node.parentNode, truncPathTree_anchorAncestor, options);
                    }
                    var path_anchorToEle = IIRPA.locale.extractUniquePath(node, truncPathTree_anchorToEle, options);
                    path = path_anchorAncestor.concat(path_anchorToEle);
                }
            }
            // 某些情况下property.value不是字符串或者数字，会导致在C#中反序列化失败，需要过滤掉
            for (var i = 0; i < path.length; i++) {
                filterPropertyValue(path[i].props);
                //filterPropertyValue(path[i].optionProps);
            }
            //console.log("update" + IIRPA.JSON().stringify(path));
            return { 'status': IIRPA.ErrorCode.SUCCESS, 'value': IIRPA.JSON().stringify(path) };
        } catch (e) {
            retval = { 'status': e.code || IIRPA.ErrorCode.JAVASCRIPT_ERROR, 'value': e.message };
        }
        return retval || {};
    }
    IIRPA.locale.extractUniquePathByIgnoreAnchor = function (node, pathTree, options) {
        if (!pathTree || pathTree.length == 0) {
            return pathTree;
        }
        var cloneOptions = IIRPA.JSON().parse(IIRPA.JSON().stringify(options));
        cloneOptions.ignoreIdAnchor = true;
        var path_anchorAncestor = IIRPA.locale.extractUniquePath(node, pathTree, cloneOptions);
        for (var i = 0; i < path_anchorAncestor.length; i++) {
            path_anchorAncestor[i].accurate = 'false';
        }
        return path_anchorAncestor;
    }
    /**  * 找相似元素  */
    IIRPA.api.similarInspectPath = function (uniqueID, path) {
        try {
            //find element by path
            var truncPathTree = path;
            var elements = IIRPA.locale.findElements(truncPathTree);
            if (!elements || elements.length == 0) {
                return { 'status': IIRPA.ErrorCode.NO_SUCH_ELEMENT, 'value': '未找到控件' };
            }
            //find newElement     
            var newElement = uniqueID;
            if (!newElement) {
                return { 'status': IIRPA.ErrorCode.NO_SUCH_ELEMENT, 'value': '未找到控件' };
            }
            //if newElement in elements         
            var resultPath;
            if (elements[0] == newElement) {
                resultPath = path;
            } else {
                var pathArray = IIRPA.locale.similarPath(elements[0], newElement);
                resultPath = IIRPA.JSON().stringify(pathArray);
            }
            return { 'status': IIRPA.ErrorCode.SUCCESS, 'value': resultPath };
        } catch (e) {
            retval = { 'status': e.code || IIRPA.ErrorCode.JAVASCRIPT_ERROR, 'value': e.message };
        }
        return retval || {};
    }
    /**  * 找元素  * @param {*} path   * @param {*} index   */
    IIRPA.api.findElement = function (path, index, parentIdentify) {
        index = index || 1;
        var root;
        if (parentIdentify) {
            root = IIRPA.locale.findNode(parentIdentify)
            if (!root) {
                return { 'status': IIRPA.ErrorCode.NO_SUCH_ELEMENT, 'value': '未找到父控件' };
            }
        }
        try {
            var truncPathTree = IIRPA.JSON().parse(path);
            var elements = IIRPA.locale.findElements(truncPathTree, root);
            if (elements && elements.length >= index) {
                return { 'status': IIRPA.ErrorCode.SUCCESS, 'value': elements[index - 1] };
            } return { 'status': IIRPA.ErrorCode.NO_SUCH_ELEMENT, 'value': '未找到控件' };
        } catch (e) {
            return { 'status': IIRPA.ErrorCode.INVALID_PARAM, 'value': e.message };
        }
    }
    /**  * 列表元素的操作  * @param {*} path   * @param {*} action   * @param {*} param   */
    IIRPA.api.elementListPerform = function (path, parentIdentify, actionName, actionParam) {
        IIRPA.checkEmpty(path, '控件不可为空');
        IIRPA.checkEmpty(actionName, '控件操作不可为空');
        var root;
        if (parentIdentify) {
            root = IIRPA.locale.findNode(parentIdentify);
            if (!root) {
                return { 'status': IIRPA.ErrorCode.NO_SUCH_ELEMENT, 'value': '未找到父控件' };
            }
        }
        try {
            var truncPathTree = IIRPA.JSON().parse(path);
            var elements = IIRPA.locale.findElements(truncPathTree, root);
            if (!elements || elements.length == 0) { return { 'status': IIRPA.ErrorCode.NO_SUCH_ELEMENT, 'value': '未找到控件' }; }
            //处理action         
            var retValue = "";
            switch (actionName.toLowerCase()) {
                case IIRPA.ActionName.COUNT:
                    retValue = elements.length;
                    break;
                case IIRPA.ActionName.POSITION:
                    var clientRects = IIRPA.dom.boundingClientRectList(elements);
                    retValue = IIRPA.JSON().stringify(clientRects);
                    break;
                case IIRPA.ActionName.TEXT:
                    var clientRects = IIRPA.dom.textList(elements);
                    retValue = IIRPA.JSON().stringify(clientRects);
                    break;
            }
            //success         
            return { 'status': IIRPA.ErrorCode.SUCCESS, 'value': retValue };
        } catch (e) {
            return { 'status': e.code || IIRPA.ErrorCode.INVALID_PARAM, 'value': e.message };
        }
    }
    /**  * 事件  * @param {*} uniqueID   * @param {*} eventName   */
    IIRPA.api.dispatchEvent = function (nodeIdentify, eventName) {
        try {
            var node = IIRPA.locale.findNode(nodeIdentify);
            if (node) {
                IIRPA.dom.dispatchEvent(node, eventName);
            }
        }
        catch (e) {
        }
    }
    /**  * 找表格  * @param {*} matchType   * @param {*} matchValue   * @param {*} returnType   */
    IIRPA.api.findTable = function (matchType, matchValue, returnType, parentIdentify) {
        try {
            var parent
            if (parentIdentify)
                parent = IIRPA.locale.findNode(parentIdentify);
            var tableData = IIRPA.locale.findTable(matchType, matchValue, returnType, parent);
            //success        
            return { 'status': IIRPA.ErrorCode.SUCCESS, 'value': IIRPA.JSON().stringify(tableData) };
        } catch (e) {
            return { 'status': e.code || IIRPA.ErrorCode.INVALID_PARAM, 'value': e.message };
        }
    }
    /**  * 根据表格对象，获取表格信息  * @param {*} uniqueID   * @param {*} returnType   */
    IIRPA.api.tableData = function (nodeIdentify, returnType) {
        var table = IIRPA.locale.findNode(nodeIdentify);
        if (!table) {
            return { 'status': IIRPA.ErrorCode.NO_SUCH_ELEMENT, 'value': '未找到表格控件' };
        } try {
            var tableData = IIRPA.dom.tableData(table, returnType);
            //success    
            return { 'status': IIRPA.ErrorCode.SUCCESS, 'value': IIRPA.JSON().stringify(tableData) };
        } catch (e) {
            return { 'status': e.code || IIRPA.ErrorCode.INVALID_PARAM, 'value': e.message };
        }
    }
    /**  * 获取滚动条信息  * @param {*} uniqueID   * @param {*} direction   */
    IIRPA.api.getScroll = function (node, direction) {
        try {
            var retValue;
            node = node || document.documentElement;
            switch (direction) {
                case "left":
                    retValue = IIRPA.dom.getScrollLeft(node);
                    break;
                case "top":
                    retValue = IIRPA.dom.getScrollTop(node);
                    break;
                default:
                    return {
                        'status': IIRPA.ErrorCode.INVALID_PARAM,
                        'value': '未知的滚动条方向' + direction
                    };
            }
            //success        
            return { 'status': IIRPA.ErrorCode.SUCCESS, 'value': retValue };
        } catch (e) {
            return { 'status': e.code || IIRPA.ErrorCode.INVALID_PARAM, 'value': e.message };
        }
    }
    /**  * 设置滚动条高/宽度  * @param {*} uniqueID   * @param {*} direction   * @param {*} value   */
    IIRPA.api.scroll = function (nodeIdentify, direction, value) {
        try {
            var retValue;
            var node;
            if (nodeIdentify) {
                node = IIRPA.locale.findNode(nodeIdentify);
                if (!node) {
                    return {
                        'status': IIRPA.ErrorCode.NO_SUCH_ELEMENT,
                        'value': '未找到控件'
                    };
                }
            }
            switch (direction) {
                case "left":
                    retValue = IIRPA.dom.scrollLeft(node, value);
                    break;
                case "top":
                    retValue = IIRPA.dom.scrollTop(node, value);
                    break;
                default:
                    return {
                        'status': IIRPA.ErrorCode.INVALID_PARAM,
                        'value': '未知的滚动条方向' + direction
                    };
            }
            //success
            return {
                'status': IIRPA.ErrorCode.SUCCESS,
                'value': retValue
            };
        } catch (e) {
            return {
                'status': e.code || IIRPA.ErrorCode.INVALID_PARAM,
                'value': e.message
            };
        }
    }
    IIRPA.api.scroll_v2 = function (node, direction, scrollType, targetNode, offSetX, offSetY) {
        try {
            var retValue;
            var node = node;
            var targetNode = targetNode;
            var value;
            //横向滚动条
            if (direction == "left") {
                if (scrollType == "bottom" || scrollType == "top") {
                    return {
                        'status': IIRPA.ErrorCode.INVALID_PARAM,
                        'value': '滚动位置无法实现'
                    };
                }
                //滚动最左侧
                if (scrollType == "left") {
                    value = 0;
                }
                //滚动最右侧
                if (scrollType == "right") {
                    if (node) {
                        // 获取区域内容宽度
                        var contentWidth = node.scrollWidth;
                        // 获取区域可视宽度
                        var viewportWidth = node.clientWidth;
                        // 计算最右侧的滚动位置
                        var rightScrollPosition = contentWidth - viewportWidth;
                        if (rightScrollPosition < 0) {
                            rightScrollPosition = 0;
                        }
                        value = rightScrollPosition;
                    }
                    else {
                        // 获取文档的总宽度
                        var documentWidth = Math.max(
                            document.body.scrollWidth,
                            document.documentElement.scrollWidth
                        );
                        // 获取视口宽度
                        var viewportWidth = window.innerWidth || document.documentElement.clientWidth || document.body.clientWidth;
                        // 计算最右侧的滚动位置
                        var rightScrollPosition = documentWidth - viewportWidth;
                        if (rightScrollPosition < 0) {
                            rightScrollPosition = 0;
                        }
                        value = rightScrollPosition;
                    }
                }
                //滚动一屏
                if (scrollType == "onescreen") {
                    if (node) {
                        // 获取区域可视宽度
                        var viewportWidth = node.clientWidth;
                        value = node.scrollLeft + viewportWidth;
                    }
                    else {
                        /// 获取视口宽度
                        var viewportWidth = window.innerWidth || document.documentElement.clientWidth || document.body.clientWidth;
                        value = document.documentElement.scrollWidth + viewportWidth;
                    }
                }
                //滚动到指定位置
                if (scrollType == "cusposition") {
                    value = offSetX;
                }
                //滚动到指定元素位置
                if (scrollType == "cuselement") {
                    //和现有滚动网页到指定元素组件实现保持一致
                    IIRPA.api.scrollIntoViewIfNeededorscrollIntoView(targetNode);
                    return {
                        'status': IIRPA.ErrorCode.SUCCESS,
                        'value': 1
                    };
                }
                retValue = IIRPA.dom.scrollLeft(node, value);
            }
            //纵向滚动条
            if (direction == "top") {
                if (scrollType == "left" || scrollType == "right") {
                    return {
                        'status': IIRPA.ErrorCode.INVALID_PARAM,
                        'value': '滚动位置无法实现'
                    };
                }
                //滚动到底部
                if (scrollType == "bottom") {
                    if (node) {
                        var contentHeight = node.scrollHeight;
                        var viewportHeight = node.clientHeight;
                        if (contentHeight - viewportHeight < 0) {
                            value = 0;
                        } else {
                            value = contentHeight - viewportHeight;
                        }
                    }
                    else {
                        var documentHeight = Math.max(
                            document.body.scrollHeight,
                            document.documentElement.scrollHeight
                        );
                        var viewportHeight = window.innerHeight || document.documentElement.clientHeight || document.body.clientHeight;
                        if (documentHeight - viewportHeight < 0) {
                            value = 0;
                        } else {
                            value = documentHeight - viewportHeight;
                        }
                    }
                }
                //滚动到顶部
                if (scrollType == "top") {
                    value = 0;
                }
                //滚动一屏
                if (scrollType == "onescreen") {
                    if (node) {
                        //垂直
                        // 获取区域可视高度
                        var viewportHeight = node.clientHeight;
                        value = node.scrollTop + viewportHeight;
                    }
                    else {
                        // 获取视口高度-垂直
                        var viewportHeight = window.innerHeight || document.documentElement.clientHeight || document.body.clientHeight;
                        value = document.documentElement.scrollTop + viewportHeight;
                    }
                }
                //滚动指定位置
                if (scrollType == "cusposition") {
                    value = offSetY;
                }
                //滚动到指定元素位置
                if (scrollType == "cuselement") {
                    //和现有滚动网页到指定元素组件实现保持一致
                    IIRPA.api.scrollIntoViewIfNeededorscrollIntoView(targetNode);
                    return {
                        'status': IIRPA.ErrorCode.SUCCESS,
                        'value': 1
                    };
                }
                retValue = IIRPA.dom.scrollTop(node, value);
            }
            //success
            return {
                'status': IIRPA.ErrorCode.SUCCESS,
                'value': retValue
            };
        } catch (e) {
            return {
                'status': e.code || IIRPA.ErrorCode.INVALID_PARAM,
                'value': e.message
            };
        }
    }

    IIRPA.api.scrollTo = function (node, params) {
        node = node || window;
        params.behavior = params.behavior || "auto";
        switch (params.position) {
            case "point":
                node.scrollTo({
                    top: params.top,
                    left: params.left,
                    behavior: params.behavior
                });
                break;
            case "top":
                node.scrollTo({
                    top: 0,
                    behavior: params.behavior
                });
                break;
            case "bottom":
                node.scrollTo({
                    top: node == window ? document.body.scrollHeight : node.scrollHeight,
                    behavior: params.behavior
                });
                break;
            case "distance":
                if ('scrollBehavior' in document.documentElement.style) {
                    window.scrollBy({
                        top: params.distance,
                        behavior: params.behavior
                    });
                } else {
                    document.documentElement.scrollTop += params.distance;
                    document.body.scrollTop += params.distance;
                }
                break;
            case "distanceTop":
                IIRPA.dom.scrollTop("", document.documentElement.scrollTop + params.distance);
                break;
            default:
                node.scrollTo(params);
                break;
        }
    }
    /**  *  * 判断某个元素是否在可视区域内   *     - 若不指定坐标，要是整个元素都在可见区域内  *     - 否则，坐标在可见区域内即可  * @param {*} uniqueID   * @param {*} elementLeft    * @param {*} elementTop   * @param {*} scene 场景，比如截图，那需要整个图像暴露出来,比如点击/鼠标移入，那需要中心点暴露出来，默认空时，需要整个控件暴露出来  */
    IIRPA.api.scrollIntoViewIfNeeded = function (nodeIdentify, elementLeft, elementTop, scene) {
        try {
            var node = IIRPA.locale.findNode(nodeIdentify);
            if (!node) {
                return { 'status': IIRPA.ErrorCode.NO_SUCH_ELEMENT, 'value': '未找到控件' };
            }
            if (!IIRPA.dom.elementVisibleLocation(node, elementLeft, elementTop)) {
                IIRPA.trace("need execute scrollIntoView");
                IIRPA.dom.scrollIntoView(node, scene);
            }
            //success
            return { 'status': IIRPA.ErrorCode.SUCCESS, 'value': 1 };
        } catch (e) {
            return { 'status': e.code || IIRPA.ErrorCode.INVALID_PARAM, 'value': e.message };
        }
    }
    /**  * 判断控件是否在可见区域  */
    IIRPA.api.visibleLocation = function (nodeIdentify, location, elementLeft, elementTop) {
        try {
            var node = IIRPA.locale.findNode(nodeIdentify);
            if (!node) {
                return { 'status': IIRPA.ErrorCode.NO_SUCH_ELEMENT, 'value': '未找到控件' };
            }
            //success         
            return { 'status': IIRPA.ErrorCode.SUCCESS, 'value': IIRPA.dom.elementVisibleLocation(node, location, elementLeft, elementTop) };
        } catch (e) {
            return { 'status': e.code || IIRPA.ErrorCode.INVALID_PARAM, 'value': e.message };
        }
    }
    /**  * 获取浏览器缩放比例  */
    IIRPA.api.zoomLevel = function () {
        try {
            return { 'status': IIRPA.ErrorCode.SUCCESS, 'value': IIRPA.zoomLevel() };
        } catch (e) {
            return { 'status': e.code || IIRPA.ErrorCode.INVALID_PARAM, 'value': e.message };
        }
    }
    /**  * 获取元素缩放比例  */
    IIRPA.api.elementZoom = function (ele) {
        var zoomVal = 1;
        try {
            if (IIRPA.isZoomAppliedToRect) {
                return zoomVal;
            }
            var tempDiv = document.createElement("div");
            tempDiv.style.width = "1000px";
            tempDiv.style.height = "1px";
            tempDiv.style.zoom = "0.2";
            tempDiv.style.position = "absolute";
            tempDiv.style.visibility = "hidden";
            document.body.appendChild(tempDiv);
            var rect = tempDiv.getBoundingClientRect();
            document.body.removeChild(tempDiv);
            var width = rect.width || (rect.right - rect.left);
            if (width < 300) {
                IIRPA.isZoomAppliedToRect = true;
                return zoomVal;
            }
            var pEle = ele;
            while (pEle && pEle.nodeType == 1) {
                var computedStyle = window.getComputedStyle ? window.getComputedStyle(pEle) : pEle.currentStyle;
                var zoom = computedStyle.zoom || pEle.style.zoom || '1';
                if (zoom != '1') {
                    zoomVal *= parseFloat(zoom);
                }
                if (pEle.tagName.toLowerCase() == 'body') {
                    break;
                }
                pEle = pEle.parentNode;
            }
        } catch (e) {
        }
        return zoomVal;
    }
    /**  * 获取属性  */
    IIRPA.api.attr = function (nodeIdentify, attrName) {
        try {
            if (!attrName)
                return { 'status': IIRPA.ErrorCode.INVALID_PARAM, 'value': '属性名称不可为空' };
            var node = IIRPA.locale.findNode(nodeIdentify);
            if (!node) {
                return { 'status': IIRPA.ErrorCode.NO_SUCH_ELEMENT, 'value': '未找到控件' };
            } return { 'status': IIRPA.ErrorCode.SUCCESS, 'value': IIRPA.dom.attrValue(node, attrName) };
        } catch (e) {
            return { 'status': e.code || IIRPA.ErrorCode.INVALID_PARAM, 'value': e.message };
        }
    }
    IIRPA.dom.siblings = function (n, elem) {
        var matched = [];
        for (; n; n = n.nextSibling) {
            if (n.nodeType === 1 && n !== elem) {
                matched.push(n);
            }
        }
        return matched;
    };
    IIRPA.dom.elementFromPoint = function (ptX, ptY, parentEle) {
        var ele;
        if (parentEle) {
            if (IIRPA.dom.isShadowRootNode(parentEle)) {
                ele = parentEle.elementFromPoint(ptX, ptY)
            }
            else {
                ele = parentEle;
            }
        }
        else {
            ele = document.elementFromPoint(ptX, ptY);
        }
        try {
            if (ele.shadowRoot) {
                return IIRPA.dom.elementFromPoint(ptX, ptY, ele.shadowRoot);
            }
            if (ele.children) {
                var children = ele.children;
                if (children.length > 0) {
                    var matchEle;
                    var matchEleRect;
                    for (var i = 0; i < children.length; i++) {
                        var rect = children[i].getBoundingClientRect();
                        if (rect.x <= ptX && ptX <= rect.x + rect.width && rect.y <= ptY && ptY <= rect.y + rect.height) {
                            if (!matchEle || matchEleRect.Width * matchEleRect.Height > children[i].Width * children[i].Height) {
                                matchEle = children[i];
                                matchEleRect = rect;
                            }
                        }
                    }
                    if (matchEle) {
                        return IIRPA.dom.elementFromPoint(ptX, ptY, matchEle);
                    }
                }
            }
        } catch (e) {
            IIRPA.trace("elementFromPoint:" + e.message);
        }
        return ele;
    }

    IIRPA.ShadowRootDescendants = function () {
        if (!window.ShadowRootDescendants) {
            window.ShadowRootDescendants = {};
        }
        return window.ShadowRootDescendants;
    };
    IIRPA.dom.getDeepFrameIndex = function (targetWindow) {
        var wndFrames = targetWindow.parent.frames;
        var baseIdx = wndFrames.length;
        var search = function (root) {
            var frames = root.querySelectorAll('iframe, frame');
            for (var i = 0; i < frames.length; i++) {
                var f = frames[i]
                var win = f.contentWindow;
                if (!win) continue;
                if (win === targetWindow) return baseIdx;
                if (!Array.prototype.includes.call(wndFrames, win)) ++baseIdx;
            }
            var allEle = root.querySelectorAll('*');
            for (var i = 0; i < allEle.length; i++) {
                if (allEle[i].shadowRoot) {
                    var hit = search(allEle[i].shadowRoot);
                    if (hit !== -1) return hit;
                }
            }
            return -1;
        };
        return search(targetWindow.parent.document);
    }
    IIRPA.dom.GetShadowRoot = function (ele) {
        return ele && ele.getRootNode && IIRPA.dom.isShadowRootDescendants(ele) ? ele.getRootNode() : null;
    }
    IIRPA.dom.GetShadowRootParent = function (ele) {
        return ele && ele.getRootNode && IIRPA.dom.isShadowRootDescendants(ele) ? ele.getRootNode().host : null;
    }
    IIRPA.dom.isShadowRootNode = function (ele) {
        return ele && (ele.toString() === '[object ShadowRoot]');
    }
    IIRPA.dom.isDecumentNode = function (ele) {
        return !!ele && ele.nodeType === 9;
    }
    IIRPA.dom.isShadowRootDescendants = function (ele) {
        return (ele && ele.getRootNode && ele.getRootNode()) && IIRPA.dom.isShadowRootNode(ele.getRootNode())
    }
    IIRPA.buildShadowRootPath = function () {
        return IIRPA.buildPath("shadow-root", [], [])
    }
    IIRPA.dom.uniqueId = function (frameTree, ele) {
        // uniqueId: {frameId}/{frameId}/{nodeId}/{tagName}, '1/2/66/TD'
        if (window._iirpa_recent_id === undefined) { window._iirpa_recent_id = 1 }
        var uniqueValue = ele.getAttribute(IIRPA.UNIQUE_KEY)
        if (uniqueValue != null) {
            var tokens = GetUniqueValue();
            tokens.pop()
            // 检测uniqueValue是否有效         
            if (!compareArray(tokens, frameTree)) {
                uniqueValue = null
            }
        }
        if (uniqueValue === null) {
            window._iirpa_recent_id += 1
            uniqueValue = frameTree.join('/') + "/" + window._iirpa_recent_id
            ele.setAttribute(IIRPA.UNIQUE_KEY, uniqueValue)
            if (IIRPA.dom.isShadowRootDescendants(ele))
                IIRPA.ShadowRootDescendants()[uniqueValue] = ele
        }
        return uniqueValue + "/" + ele.tagName;
        function GetUniqueValue() {
            var tokens = uniqueValue.split('/');
            var parsedTokens = [];

            for (var i = 0; i < tokens.length; i++) {
                var token = parseInt(tokens[i]);

                if (!isNaN(token)) {
                    parsedTokens.push(token);
                }
            }
            return parsedTokens;
        }
        function compareArray(arr1, arr2) {
            if (!arr2 || !arr1)
                return false;
            if (arr1.length != arr2.length)
                return false;
            for (var i = 0, l = arr1.length; i < l; i++) {
                if (arr1[i] instanceof Array && arr2[i] instanceof Array) {
                    if (!compareArray(arr1[i], arr2[i]))
                        return false;
                } else if (arr1[i] != arr2[i]) {
                    return false;
                }
            }
            return true;
        }
    }
    IIRPA.dom.getElements = function (frameTree, tagNamesStr, attrKeywordsStr) {
        var attrKeywords = attrKeywordsStr ? attrKeywordsStr.split(",") : [];
        var tagNames = tagNamesStr ? tagNamesStr.split(",") : [];
        var viewWidth = window.innerWidth || document.documentElement.clientWidth || document.body.clientWidth
        var viewHeight = window.innerHeight || document.documentElement.clientHeight || document.body.clientHeight
        var elements = [];
        if (document.querySelectorAll) {
            elements = document.querySelectorAll('body,body *');
        }
        else {
            elements = IIRPA.dom.querySelecorAllUsingSizzle("body,body *", document);
        }
        //var retElement = [];
        var retDataArr = [];
        var retData = {};
        for (var i = 0; i < elements.length; i++) {
            retData = {};
            var ele = elements[i];
            if (tagNames.length > 0 && tagNames.indexOf(ele.tagName.toLowerCase()) == -1) {
                continue;
            }
            if (IsInView(ele)) {
                if (matchAttrKeywords(ele, attrKeywords, retData)) {
                    retData.uniqueId = IIRPA.dom.uniqueId(frameTree, ele);
                    retData.controlType = ele.tagName;
                    retDataArr.push(retData);
                }

            }
        }

        return IIRPA.JSON().stringify(retDataArr);
        function IsInView(ele) {
            var eleClientRect = ele.getBoundingClientRect()
            if (eleClientRect.width == 0 || eleClientRect.height == 0) {
                return false;
            }
            var x1 = eleClientRect.x ? eleClientRect.x : eleClientRect.left;
            var y1 = eleClientRect.y ? eleClientRect.y : eleClientRect.top;
            var x2 = x1 + eleClientRect.width;
            var y2 = y1 + eleClientRect.height;
            if ((y1 >= 0 && y1 <= viewHeight) || (y2 >= 0 && y2 <= viewHeight)) {
                return (x1 >= 0 && x1 <= viewWidth) || (x2 >= 0 && x2 <= viewWidth);
            }
            return false;
        }
        function matchAttrKeywords(element, attrKeywords, data) {
            var isMatch = false;
            var attrs = element.attributes;
            if (attrKeywords.length == 0) {
                data.text = element.innerText;//无关键词过滤，默认只返回innerText
                isMatch = true;
                return isMatch;
            }
            for (var j = 0; j < attrs.length; j++) {
                if (attrKeywords.length == 0) {
                    data[attrs[j].name] = attrs[j].value;//无关键词过滤，返回所有attribute 信息
                    isMatch = true;
                    continue;
                }
                for (var k = 0; k < attrKeywords.length; k++) {
                    if ((attrs[j].value && attrs[j].value.indexOf(attrKeywords[k]) !== -1)) {
                        data[attrs[j].name] = attrs[j].value;
                        isMatch = true;
                    }
                    if ((j === 0 && element.innerText && element.innerText.indexOf(attrKeywords[k]) !== -1)) {
                        data.text = element.innerText;
                        isMatch = true;
                    }
                }
            }
            return isMatch;
        }
    }
    IIRPA.dom.IsOpacity = function (ele) {
        if (ele && ele.nodeType === 1) {
            var computedStyle = window.getComputedStyle(ele); // 获取元素的计算样式
            var opacityValue = parseFloat(computedStyle.opacity); // 提取透明度值并转换成小数类型
            if (!isNaN(opacityValue) && opacityValue == 0) {
                return true; //正确提取透明度值 1，则认定元素透明
            }
        }
        return false;
    }
    IIRPA.dom.IsInView = function (ele) {

        var viewWidth = window.innerWidth || document.documentElement.clientWidth || document.body.clientWidth
        var viewHeight = window.innerHeight || document.documentElement.clientHeight || document.body.clientHeight
        var eleClientRect = ele.getBoundingClientRect()
        if (eleClientRect.width == 0 || eleClientRect.height == 0) {
            return false;
        }
        var x1 = eleClientRect.x ? eleClientRect.x : eleClientRect.left;
        var y1 = eleClientRect.y ? eleClientRect.y : eleClientRect.top;
        var x2 = x1 + eleClientRect.width;
        var y2 = y1 + eleClientRect.height;
        if ((y1 >= 0 && y1 <= viewHeight) || (y2 >= 0 && y2 <= viewHeight)) {
            return (x1 >= 0 && x1 <= viewWidth) || (x2 >= 0 && x2 <= viewWidth);
        }
        return false;
    }
    IIRPA.dom.IsCovered = function (element) {
        var bounding = element.getBoundingClientRect();
        var x = bounding.x,
            y = bounding.top,
            width = bounding.width,
            height = bounding.height,
            bottom = bounding.bottom,
            right = bounding.right;

        var pointElement = IIRPA.dom.elementFromPoint(x + width / 2, y + height / 2);
        return pointElement !== element && !element.contains(pointElement);
        //var elements = [
        //    document.elementFromPoint(x + 2, y + 2),
        //    document.elementFromPoint(right - 2, y + 2),
        //    document.elementFromPoint(x + 2, bottom - 2),
        //    document.elementFromPoint(right - 2, bottom - 2)
        //];
        //return elements.filter((ele) => ele !== null).some((ele) => ele !== element && !element.contains(ele));
    }
    IIRPA.dom.setAllElementsUniqueId = function (frameTree) {
        var elements = [];
        var element = document.getElementsByTagName("body")[0];
        setElementsUniqueId(element);
        return frameIds;
        function setElementsUniqueId(element) {
            /* try {*/
            if (element.nodeType === 1) {
                var uniqueid = IIRPA.dom.uniqueId(frameTree, element);
                var tagName = element && element.tagName && element.tagName.toLowerCase();
                if (tagName === "frame" || tagName === "iframe") {
                    elements.push(element);
                }
            }
            child = element.firstChild;
            while (!!child) {
                if (child.nodeType !== 1 && child.nodeType !== 3) {
                    child = child.nextSibling;
                    continue;
                }
                var tagName = child.nodeType === 1 ? child.tagName.toLowerCase() : "";
                if (tagName == "script" || tagName == "style") {
                    child = child.nextSibling;
                    continue;
                }
                if (!child) {
                    setElementsUniqueId(child);
                    /* if (child.nextSibling) {*/
                    child = child.nextSibling;
                    /* }*/
                }
            }
            //}
            //catch (error) {

            //}

        }
    }

    IIRPA.dom.getViewElementsData = function (frameTree) {
        var bodyEle = document.getElementsByTagName("body")[0];
        var retData = getChiidrenData(bodyEle, [0]);
        return IIRPA.JSON().stringify(retData);

        function getChiidrenData(element, path) {
            if (IIRPA.dom.IsOpacity(element)) {
                return null;
            }
            var data = getElementData(element);
            if (!data) {
                return data;
            }
            //data.pathpath = path;
            if (element.nodeType === 1 && element.children.length == 0) {
                return data;
            }
            var child = element.firstChild;
            var idx = -1;
            while (!!child) {
                idx++;
                if (child.nodeType !== 1 && child.nodeType !== 3) {
                    child = child.nextSibling;
                    continue;
                }
                var tagName = child.nodeType === 1 ? child.tagName.toLowerCase() : "";
                if (tagName == "script" || tagName == "style") {
                    child = child.nextSibling;
                    continue;
                }
                if (child.nodeType === 1 && window.irpa && child.getAttribute("i-rpa") == window.irpa) {
                }
                var childData = getChiidrenData(child, path.concat(idx));

                if (!!childData &&
                    (child.nodeType === 3 ||
                        (child.nodeType === 1 && IIRPA.dom.IsInView(child)) ||
                        (childData.children && childData.children.length > 0))) {
                    if (childData.tagName == "" && !childData.uniqueId) {
                        childData.uniqueId = data.uniqueId + "_child_" + idx;
                        childData.bounding = data.bounding;
                    }
                    var ispush = true;
                    if (child.nodeType === 1) {
                        if (childData.children.length == 0 || (childData.children.length == 1 && childData.children[0].tagName == "")) {
                            if (!(data.tagName === 'iframe' || data.tagName === 'frame') && IIRPA.dom.IsCovered(child)) {
                                ispush = false;
                            }
                        }
                    }
                    if (ispush) {
                        data.children.push(childData);
                    }
                }
                child = child.nextSibling;
            }
            if (data.children.length == 1 && data.children[0].tagName == "" && (data.specific.text.length > 0 || !IIRPA.dom.IsInView(element))) {
                data.children = [];
            }
            else if (data.children.length > 0) {
                var computedStyle = window.getComputedStyle(element);
                var eleOverflow = computedStyle.overflow;
                if (computedStyle.position === "fixed") {
                    data.position = "fixed";
                }
                for (var i = data.children.length - 1; i >= 0; i--) {
                    if (data.children[i].hadFixedChildren || data.children[i].position === "fixed") {
                        data.hadFixedChildren = true;
                    }
                    if (!data.hadFixedChildren && data.children[i].bounding && isOverlap(data.bounding, data.children[i].bounding)) {
                        if (eleOverflow.indexOf("hidden") !== -1 || eleOverflow.indexOf("auto") !== -1 || eleOverflow.indexOf("scroll") !== -1) {
                            data.children.splice(i, 1);
                        }
                    }
                }
            }
            return data;
        }

        function isOverlap(boundingP, boundingC) {
            return boundingP.x + boundingP.width < boundingC.x ||
                boundingP.x > boundingC.x + boundingC.width ||
                boundingP.y + boundingP.height < boundingC.y ||
                boundingP.y > boundingC.y + boundingC.height
        }
        function getElementData(ele) {
            var data = null;
            if (ele.nodeType === 1) {
                var clientRect = IIRPA.dom.boundingClientRect(ele);
                var specific = getElementSpecific(ele);
                data = {
                    uniqueId: IIRPA.dom.uniqueId(frameTree, ele),
                    tagName: ele.tagName.toLowerCase(),
                    specific: specific,
                    bounding: {
                        x: parseInt(clientRect.left),
                        y: parseInt(clientRect.top),
                        width: parseInt(clientRect.width),
                        height: parseInt(clientRect.height)
                    },
                    children: []
                }
            }
            else if (ele.nodeType === 3) {
                var nodeVal = ele.nodeValue.replace(/\s/g, '');
                if (nodeVal && nodeVal.length < 50) {
                    data = {
                        tagName: "",
                        specific: {
                            text: nodeVal
                        },
                        children: []
                    }
                }
            }
            return data;
        }

        function getElementSpecific(element) {
            var specific = {};
            var tagName = element.tagName.toLowerCase();
            if (element.title && element.title.length > 0) {
                specific.title = element.title;
            }
            var ariaLable = element.getAttribute("aria-lable");
            if (!!ariaLable && ariaLable.length > 0) {
                specific.ariaLable = ariaLable;
            }
            var placeholder = element.getAttribute("placeholder");
            if (!!placeholder) {
                specific.placeholder = placeholder;
            }
            if (tagName == "input") {
                specific.type = element.type;
                specific.text = element.value;
            }
            else if (tagName == "textarea") {
                specific.text = element.value;
            }
            else {
                specific.text = element.children.length == 0 ? element.innerText : "";
            }
            if (specific.text && specific.text.length >= 100) {
                specific.text = "";
            }
            return specific;
        }


    }

    IIRPA.SelectorGenerateor = (function () {
        var SelectorGenerateor = function (target, options) {
            this.options = options || {}
            this.target = target;
            this.rounds = ['additionRound', 'classRound', 'indexRound', 'clearRound'];
            if (options.excludeInnerText && options.excludeInnerText == true) {
                this.additionFiltersType = ["src"]
            }
            else {
                this.additionFiltersType = ["innerText", "src"]
            }
        };

        // Common
        var assign = function (target, varArgs) {
            var to = Object(target);
            for (var index = 1; index < arguments.length; index++) {
                var nextSource = arguments[index];
                if (nextSource != null) {
                    for (var nextKey in nextSource) {
                        if (Object.prototype.hasOwnProperty.call(nextSource, nextKey)) {
                            to[nextKey] = nextSource[nextKey];
                        }
                    }
                }
            }
            return to;
        };
        var forEach = function (list, fn, context) {
            for (var k = 0, length = list.length; k < length; k++) {
                if (typeof fn === "function" && Object.prototype.hasOwnProperty.call(list, k)) {
                    fn.call(context, list[k], k, list);
                }
            }
        };
        var filter = function (list, fn, context) {
            var arr = [];
            if (typeof fn === "function") {
                for (var k = 0, length = list.length; k < length; k++) {
                    fn.call(context, list[k], k, list) && arr.push(list[k]);
                }
            }
            return arr;
        };
        var find = function (list, fn, context) {
            return filter(list, fn, context)[0];
        };
        var map = function (list, fn, context) {
            var arr = [];
            if (typeof fn === "function") {
                for (var k = 0, length = list.length; k < length; k++) {
                    arr.push(fn.call(context, list[k], k, list));
                }
            }
            return arr;
        };
        var indexOf = function (list, val) {
            for (var k = 0, length = list.length; k < length; k++) {
                if (list[k] === val) {
                    return k;
                }
            }
            return -1;
        };
        var includes = function (list, val) {
            return indexOf(list, val) > -1;
        };
        var some = function (list, fn, context) {
            var passed = false;
            if (typeof fn === "function") {
                for (var k = 0, length = list.length; k < length; k++) {
                    if (passed === true) break;
                    passed = !!fn.call(context, list[k], k, list);
                }
            }
            return passed;
        };
        var every = function (list, fn, context) {
            var passed = true;
            if (typeof fn === "function") {
                for (var k = 0, length = list.length; k < length; k++) {
                    if (passed === false) break;
                    passed = !!fn.call(context, list[k], k, list);
                }
            }
            return passed;
        };
        var reduce = function (list, callback, initialValue) {
            var previous = initialValue, k = 0, length = list.length;
            if (typeof initialValue === "undefined") {
                previous = list[0];
                k = 1;
            }

            if (typeof callback === "function") {
                for (k; k < length; k++) {
                    Object.prototype.hasOwnProperty.call(list, k) && (previous = callback(previous, list[k], k, list));
                }
            }
            return previous;
        };
        var addOrReplace = function (list, removeItem, newItem) {
            var idx = indexOf(list, removeItem);
            idx > -1 ? list.splice(idx, 1, newItem) : list.push(newItem);
        };
        var trim = function (str) {
            return str.replace(/^[\s\uFEFF\xA0]+|[\s\uFEFF\xA0]+$/g, '');
        };
        var toArray = function (iterator) {
            var arr = [];
            for (var k = 0, length = iterator.length; k < length; k++) {
                arr.push(iterator[k]);
            }
            return arr;
        };
        var uniq = function (list) {
            var ra = new Array();
            for (var i = 0; i < list.length; i++) {
                if (indexOf(ra, list[i]) < 0) {
                    ra.push(list[i]);
                }
            }
            return ra;
        };
        var uniqArrayEqual = function (array1, array2) {
            return array1.length === array2.length && every(array1, function (i) { return includes(array2, i); });
        };
        var flat = function (array) {
            return reduce(array, function (res, i) {
                return res.concat(i);
            }, []);
        };
        var intersect = function (array1, array2) {
            return filter(uniq(array1), function (v) { return includes(array2, v); });
        };
        var minus = function (target, compare) {
            return filter(uniq(target), function (v) { return !includes(compare, v); });
        };
        var cloneArray = function (target) {
            return Array.prototype.slice.call(target);
        };
        // Common end

        var isIgnoredClass = function (cls) {
            var reg = /hover|over|^in$|active|inactive|selected|open|closed|focus|clearfix|enable|disable/i;
            return reg.test(cls);
        };

        var getId = function (el) {
            var elId = el.getAttribute('id');
            return elId && IIRPA.dom.usefulId(el) ? elId : '';
        };
        var getTagName = function (el) {
            return IIRPA.cssPathEscape(IIRPA.dom.lowerCaseNodeName(el));
        };
        var getClasses = function (el) {
            if (!IIRPA.dom.nodeHasAttribute(el, 'class')) {
                return [];
            }
            var classAttr = ((IIRPA.getBrowserType() === 'ie' && IIRPA.getIeVersion() < 8) ? el.className : el.getAttribute('class')) || '';
            var classList = uniq(classAttr.split(new RegExp('[\11\12\14\15\40]')));
            return filter(classList, function (c) { return !!c && !isIgnoredClass(c); });
        };
        var getInnerText = function (el) {
            if (IIRPA.dom.getChildCount(el) === -1)
                return trim(((el.innerText || el.outerText) || ''));
            return '';
        };
        var trySetAttribute = function (el, attr, target) {
            if (!IIRPA.dom.nodeHasAttribute(el, attr)) {
                return;
            }
            var v = el.getAttribute(attr);
            if (IIRPA.isString(v)) {
                target[attr] = v;
            }
        };

        var getAllNodes = function (child, stopIf) {
            stopIf = stopIf || function () { return false; };
            var nodes = [child]
            while (child.parentNode && !includes(['', 'html', '\\#document', '\\#document-fragment'], getTagName(child.parentNode)) && !stopIf(child)) {
                nodes.unshift(child.parentNode)
                child = child.parentNode
            }
            return map(nodes, function (e) {
                var wrap = {
                    classes: getClasses(e),
                    id: isIdUnique(e) ? getId(e) : '',
                    tag: getTagName(e),
                    $el: e
                };
                wrap.tag === 'input' && trySetAttribute(e, 'type', wrap);
                return wrap;
            })
        };

        var BASE_PROPS = ['id', 'tag'];
        var SPECIAL_PROPS = ['type']; // 'name', 'src'

        var genProps = function (n) {
            var attrHandle = function (node, attr) { return node[attr] === undefined ? '' : '[' + attr + '="' + (node[attr] || '') + '"]'; };
            var base = n.tag;
            var id = n.id ? '#' + IIRPA.escapeIdentifierIfNeeded(n.id) : '';
            var classes = n.classes ? map(n.classes, function (c) { return '.' + IIRPA.escapeIdentifierIfNeeded(c); }).join('') : '';
            var index = n.index === undefined ? '' : ':nth-child(' + n.index + ')';
            var attrs = map(SPECIAL_PROPS, function (a) { return attrHandle(n, a); }).join('');
            return base + id + classes + index + attrs;
        }

        var copyProps = function (props, attrs) {
            var out = {};
            forEach(attrs, function (attr) {
                if (props[attr] != undefined) {
                    out[attr] = props[attr]
                }
            });
            return out;
        };

        var genSelector = function (nodes, trans) {
            trans = trans || function (n) { return n; };
            nodes = map(nodes, trans);
            return map(nodes, genProps).join('>');
        };

        var genAttrs = function (nodes, include, exclude, trans) {
            trans = trans || function (n) { return n; };
            return map(nodes, function (n) {
                return copyProps(trans(n), minus(BASE_PROPS.concat(SPECIAL_PROPS).concat(include), exclude));
            });
        };

        var genAttrSelector = function (nodes, include, exclude, trans) {
            return genSelector(genAttrs(nodes, include, exclude, trans));
        };

        var getFilters = function (targetPath, startWithId, inShadowRoot) {
            return reduce(targetPath, function (result, n, level) {
                if (level === 0 && (startWithId || !inShadowRoot)) { return result; }
                return result.concat(map(n.classes, function (c) {
                    return {
                        level: level,
                        name: c,
                        type: 'class'
                    };
                }))
            }, []);
        };

        var getIndexFilters = function (targetPath, startWithId, inShadowRoot) {
            return reduce(targetPath, function (result, n, level) {
                if (level === 0 && (startWithId || !inShadowRoot)) { return result; }
                result.push({
                    level: level,
                    index: n.index,
                    type: 'index'
                });
                return result;
            }, []);
        };

        var getEffectiveNode = function (turns) {
            var copy = cloneArray(turns);
            while (copy.length) {
                var lastTurn = copy.pop();
                var node = find(lastTurn, function (i) { return i.effective; });
                if (node) {
                    return node;
                }
            }
        };

        var isSamePath = function (path1, path2) {
            if (path1.length !== path2.length) {
                return false;
            }
            for (var i = path1.length - 1; i >= 0; i--) {
                if (!(path2[i].$el === path1[i].$el)) {
                    return false;
                }
            }
            return true;
        };

        var fillIndex = function (path) {
            return forEach(path, function (node) {
                node.index = IIRPA.dom.getNthChild(node.$el);
            });
        }

        var analyseResult = function (remain, parents, target) {
            var includeTarget = !!find(remain, function (p) { return isSamePath(p, target); });
            // parents.every(function (p) { return p.length > remain.length; }) && includeTarget,
            // 以every逻辑判断是否有效会受filter加入顺序影响, 即当C为B子集, 且{ A, B } 加入 C effective结论为true, 
            // 但 { A, C } 加入 B 时, 为false, 结论不一致, 但 { A, B, C } 作为key时却认为是同一个
            // 以some为逻辑会导致无效的filter进入下一层, 但不影响found结论(如存在总会在上一层被找到), 无效的filter将在clear轮被过滤
            return {
                effective: some(parents, function (p) { return p.length > remain.length; }) && includeTarget,
                found: remain.length === 1 && includeTarget
            };
        };

        var groupByCompare = function (list, compare) {
            var source = cloneArray(list);
            var result = [];
            forEach(source, function (item) {
                var theSame = find(result, function (i) {
                    return compare(i.sample, item);
                });
                if (theSame) {
                    theSame.group.push(item);
                } else {
                    result.push({
                        group: [item],
                        sample: item
                    });
                }
            });
            return result;
        };

        var turn0Gen = function (paths, targetPath, filters, compare) {
            return map(filters, function (f) {
                var parent = paths;
                var remain = filter(parent, function (p) { return compare(p, f); });
                // 页面元素变动导致目标元素无法找到
                if (remain.length < 1) {
                    throw 'Unknown classes selector error';
                }
                return assign({
                    remain: remain,
                    turn: 0,
                    parents: [parent],
                    filters: [f]
                }, analyseResult(remain, [parent], targetPath));
            });
        };

        var turnLoop = function (results, targetPath, turn0) {
            var turn = 0, lastTurn;
            while (results.length) {
                lastTurn = results[results.length - 1];
                var found = find(lastTurn, function (i) { return i.found; });
                if (found) {
                    IIRPA.trace('Find target path at turn ' + found.turn, results);
                    return {
                        found: found,
                        turns: results
                    };
                }
                // 返回结果中保留完整层结果, 实际运行时只考虑有效节点
                lastTurn = filter(lastTurn, function (i) { return i.effective; });
                // 上次运行有效的节点小于2(无法交叉)结束
                if (lastTurn.length < 2) {
                    IIRPA.trace('Could not find target path');
                    break;
                }
                var effectiveFilters = uniq(flat(map(lastTurn, function (i) { return i.filters; })));
                turn++;
                var thisTurn = [];
                forEach(lastTurn, function (t) {
                    // 未加入的class属性
                    var otherFilter = minus(effectiveFilters, t.filters);
                    forEach(otherFilter, function (c) {
                        var currentFilter = cloneArray(t.filters).concat([c]);
                        var alreadyExist = !!find(thisTurn, function (i) { return uniqArrayEqual(i.filters, currentFilter); });
                        if (alreadyExist) {
                            return;
                        }
                        // 第0轮过滤出的子集
                        var turn0Node = find(turn0, function (i) { return includes(i.filters, c); });
                        // 当前过滤器与新加入过滤器交集
                        var remain = intersect(turn0Node.remain, t.remain);
                        thisTurn.push(assign({
                            remain: remain,
                            turn: turn,
                            parents: [turn0Node.remain, t.remain],
                            filters: currentFilter
                        }, analyseResult(remain, [turn0Node.remain, t.remain], targetPath)));
                    });
                })
                results.push(thisTurn);
            }
            return {
                turns: results
            };
        }

        var filterCompare = function (p, f) {
            switch (f.type) {
                case 'class':
                    return includes(p[f.level].classes, f.name);
                case 'index':
                    return p[f.level].index === f.index;
                case 'innerText':
                    return getInnerText(p[f.level].$el) === f.innerText;
                case 'src':
                    if (!IIRPA.dom.frameElement(p[f.level].$el)) {
                        return false;
                    }
                    switch (f.filterAction) {
                        case 'contains':
                            return p[f.level].$el.src.indexOf(f.src) > -1;
                        default:
                            return p[f.level].$el.src === f.src;
                    }
                default:
                    throw 'Unsupported filter';
            }
        };

        var minFilters = function (paths, filters, targetPath, tag) {
            var loopOver;
            if (paths.length === 1) {
                loopOver = {
                    found: {
                        remain: paths,
                        turn: -1,
                        parents: [],
                        filters: [],
                        effective: true,
                        found: true
                    },
                    turns: []
                };
            } else {
                var results = [];
                var turn0 = turn0Gen(paths, targetPath, filters, filterCompare);
                // 优化: 合并作用相同的过滤器
                var grouped = groupByCompare(turn0, function (item1, item2) {
                    return uniqArrayEqual(item1.remain, item2.remain);
                });
                turn0 = map(grouped, function (g) { return g.sample; });
                results.push(turn0);
                loopOver = turnLoop(results, targetPath, turn0);
            }
            if (loopOver.found) {
                loopOver.effectiveFilters = loopOver.found.filters;
            } else {
                var effectiveNode = getEffectiveNode(loopOver.turns)
                // TODO: 优化以下代码
                // 仅当无found结论时, effectiveFilters过滤结果等效于全量filters
                // if (effectiveNode) {
                //    if (effectiveNode.filters.length != loopOver.turns[0].filter(function(n) { return n.effective; }).length) {
                //        throw 'Effective filters number error'
                //    }
                // }
                loopOver.effectiveNode = effectiveNode;
                loopOver.effectiveFilters = effectiveNode ? effectiveNode.filters : [];
            }
            loopOver[tag + 'Filters'] = loopOver.effectiveFilters;
            return loopOver;
        }

        var minClasses = function (paths, filters, targetPath) {
            return minFilters(paths, filters, targetPath, 'class');
        };

        var minIndexes = function (paths, filters, targetPath) {
            return minFilters(paths, filters, targetPath, 'index');
        };

        var minAddition = function (paths, filters, targetPath) {
            return minFilters(paths, filters, targetPath, 'addition');
        };

        SelectorGenerateor.prototype.tracePath = function (tails) {
            var targetRoot = this.targetRoot;
            return map(toArray(tails), function (e) { return getAllNodes(e, function (el) { return targetRoot.$el === el; }); });
        };

        SelectorGenerateor.prototype.previousList = function (previous) {
            return previous.effectiveNode ? previous.effectiveNode.remain : this.similarList;
        };

        SelectorGenerateor.prototype.classRound = function (previous) {
            if (previous.found) {
                return previous;
            }

            var targetPath = this.targetPath;
            var similarList = this.previousList(previous);
            var filters = this.classFilters;

            var minClassResult = minClasses(similarList, filters, targetPath);

            this.classRoundResult = {
                similarList: similarList,
                result: minClassResult
            };

            if (!minClassResult.found && !minClassResult.effectiveNode) {
                return previous;
            }

            restoreResult(minClassResult, previous, 'class');

            return minClassResult;
        };

        SelectorGenerateor.prototype.additionRound = function (previous) {
            if (previous.found) {
                return previous;
            }

            var target = this.target;
            var targetPath = this.targetPath;

            var additionFilters = [];
            if (indexOf(this.additionFiltersType, "innerText") != -1) {
                var innerTextFilter = {
                    level: indexOf(map(targetPath, function (n) { return n.$el; }), target),
                    innerText: getInnerText(target),
                    type: 'innerText'
                };
                // innerText不为空且小于20个字符
                if (innerTextFilter.innerText && innerTextFilter.innerText.length < 20) {
                    additionFilters.push(innerTextFilter);
                }
            }
            if (indexOf(this.additionFiltersType, "src") != -1) {
                var srcFilters = filter(map(targetPath, function (n) {
                    if (IIRPA.dom.frameElement(n.$el)) {
                        return {
                            filterAction: 'contains',
                            level: indexOf(targetPath, n),
                            src: IIRPA.dom.truncSrcAttr(n.$el.src),
                            type: 'src'
                        };
                    }
                }), function (f) { return f; });
                additionFilters = additionFilters.concat(srcFilters);
            }
            this.additionFilters = additionFilters;
            if (this.additionFilters.length < 1) {
                return previous;
            }

            var previousList = this.previousList(previous);
            var minAdditionResult = minAddition(previousList, additionFilters, targetPath);

            this.additionRoundResult = {
                similarList: previousList,
                result: minAdditionResult
            };

            if (!minAdditionResult.found && !minAdditionResult.effectiveNode) {
                return previous;
            }

            restoreResult(minAdditionResult, previous, 'addition');

            return minAdditionResult;
        };

        SelectorGenerateor.prototype.clearRound = function (previous) {
            // 在上次清洗过之后没有重新生成, 或者没有找到目标过滤器, 完整传递到下层
            if (previous.cleared || !previous.found) {
                return previous;
            }

            var allFilters = collectFilters(previous);
            var flatFilters = flat(allFilters);
            // 清洗只对多轮过滤器组合生成filters
            if (some(allFilters, function (f) {
                return f.length != 0 && f.length < flatFilters.length;
            })) {
                var targetPath = this.targetPath;
                var similarList = this.similarList;

                var clearResult = minFilters(similarList, flatFilters, targetPath, 'clear');

                if (!clearResult.found) {
                    throw 'Unknown clear selector error';
                }

                // 当前结构clear轮不会运行2次
                this.clearRoundResults = {
                    result: clearResult
                };

                var effectiveFilters = clearResult.effectiveFilters;
                loopAllFilters(previous, function (filters, filterName) {
                    previous[filterName] = filter(filters, function (f) {
                        return includes(effectiveFilters, f);
                    });
                });
            }

            previous.cleared = true;
            return previous;
        };

        var loopAllFilters = function (previous, callback) {
            return map(['class', 'addition', 'index'], function (tag) {
                var filterName = tag + 'Filters';
                return callback(previous[filterName] || [], filterName, tag);
            });
        };

        var collectFilters = function (previous) {
            return loopAllFilters(previous, function (filters) { return filters; });
        };

        var restoreResult = function (target, from, exclude) {
            loopAllFilters(from, function (filters, filterName, tag) {
                if (tag === exclude) {
                    return;
                } else {
                    target[filterName] = filters
                }
            });
        };

        var isIdUnique = function (e) {
            return IIRPA.dom.usefulId(e) && IIRPA.dom.checkIdUnique(getTagName(e), e.getAttribute('id'), IIRPA.dom.getRootNode(e));
        };
        SelectorGenerateor.prototype.indexRound = function (previous) {
            if (previous.found) {
                return previous;
            }

            var targetPath = this.targetPath;
            var previousList = this.previousList(previous);

            forEach(this.similarList, function (path) { fillIndex(path); });
            fillIndex(targetPath);
            var filters = getIndexFilters(targetPath, this.startWithId, this.inShadowRoot);
            this.indexFilters = filters;

            var minIndexResult = minIndexes(previousList, filters, targetPath);

            this.indexRoundResult = {
                similarList: previousList,
                result: minIndexResult
            };

            if (!minIndexResult.found) {
                throw 'Can not find path at index round';
            } else {
                restoreResult(minIndexResult, previous, 'index');
            }

            return minIndexResult;
        };

        SelectorGenerateor.prototype.gen = function (truncPathTree) {
            var self = this;
            var target = this.target;
            var stopif = this.options.ignoreIdAnchor ? null : isIdUnique;

            var targetPath = getAllNodes(target, stopif);
            var targetRoot = targetPath[0];

            var inShadowRoot = IIRPA.dom.isShadowRootDescendants(targetRoot.$el);
            var startWithId = false;
            if (!this.options.ignoreIdAnchor) {
                startWithId = !!find(truncPathTree[0].props, function (p) { return p.name === IIRPA.dom.Attr.ID && p.accurate === 'true'; });
            }

            var classFilters = getFilters(targetPath, startWithId, inShadowRoot);


            assign(this, {
                targetPath: targetPath,
                classFilters: classFilters,
                targetRoot: targetRoot,
                truncPathTree: truncPathTree,
                startWithId: startWithId,
                inShadowRoot: inShadowRoot
            });
            var baseSelector = genAttrSelector(targetPath, [], []);
            var shadowRootNode = IIRPA.dom.GetShadowRoot(target);
            var allSimilarEles = IIRPA.dom.querySelectorAll(baseSelector, shadowRootNode);

            var visibleSimilarEles = [];
            if (self.options.excludeHiddenEle) {
                var discernHidden = IIRPA.dom.IsElementHidden(target, true)
                if (!discernHidden)
                    visibleSimilarEles = filter(allSimilarEles, function (e) { return !IIRPA.dom.IsElementHidden(e, true) })
                var prop = IIRPA.buildProp(IIRPA.dom.Attr.ELEVISIBLE, (!discernHidden).toString(), IIRPA.PropPattern.EQUAL, false)
                addOrReplace(truncPathTree[truncPathTree.length - 1].props, null, prop);
            }
            
            var similarList = this.tracePath(visibleSimilarEles.length > 0 && visibleSimilarEles || allSimilarEles);
            assign(this, {
                baseSelector: baseSelector,
                similarList: similarList
            });

            var clearIndexResult = reduce(this.rounds, function (result, roundName) {
                return self[roundName].call(self, result);
            }, {});

            var targetFilters = flat(collectFilters(clearIndexResult));
            var filterPath = genAttrs(targetPath, ['classes', 'index', 'innerText', 'src'], [], function (e) {
                e.classes = map(filter(targetFilters, function (f) { return f.type === 'class' && f.level === indexOf(targetPath, e); }), function (f) { return f.name; });
                e.index = map(filter(targetFilters, function (f) { return f.type === 'index' && f.level === indexOf(targetPath, e); }), function (f) { return f.index; })[0];
                e.innerText = map(filter(targetFilters, function (f) { return f.type === 'innerText' && f.level === indexOf(targetPath, e); }), function (f) { return f.innerText; })[0];
                e.src = map(filter(targetFilters, function (f) { return f.type === 'src' && f.level === indexOf(targetPath, e); }), function (f) { return f.src; })[0];
                return e;
            });

            return {
                filters: targetFilters,
                filterPath: filterPath,
                selector: genSelector(filterPath),
                transTree: function (truncPathTree) {
                    var startWithId = false;
                    var isShadowRootDescendants = IIRPA.dom.isShadowRootDescendants(self.targetRoot.$el);
                    if (!self.options.ignoreIdAnchor) {
                        startWithId = !!find(truncPathTree[0].props, function (p) { return p.name === IIRPA.dom.Attr.ID && p.accurate === 'true'; });
                    }
                    forEach(truncPathTree, function (n) {
                        forEach(n.props, function (p) {
                            p.accurate = 'false';
                        });
                    });
                    forEach(map(filterPath, function (n) {
                        n[IIRPA.dom.Attr.TEXT] = n.innerText;
                        return n;
                    }), function (f, idx) {
                        idx = startWithId || isShadowRootDescendants ? idx : idx - 1;
                        var trunc = truncPathTree[idx], prop;
                        if (trunc) {
                            forEach([IIRPA.dom.Attr.ID, IIRPA.dom.Attr.TYPE, IIRPA.dom.Attr.SRC, IIRPA.dom.Attr.TEXT], function (attr) {
                                if (f[attr]) {
                                    prop = find(trunc.props, function (p) { return p.name === attr; })
                                    if (attr === IIRPA.dom.Attr.SRC) {
                                        addOrReplace(trunc.props, prop, IIRPA.buildProp(attr, f[attr], IIRPA.PropPattern.CONTAIN, true))
                                    } else {
                                        addOrReplace(trunc.props, prop, IIRPA.buildProp(attr, f[attr], IIRPA.PropPattern.EQUAL, true))
                                    }
                                }
                            });
                            if (f.index != undefined) {
                                prop = find(trunc.props, function (p) { return p.name === IIRPA.dom.Attr.NTH_CHILD; })
                                addOrReplace(trunc.props, prop, IIRPA.buildProp(IIRPA.dom.Attr.NTH_CHILD, f.index, IIRPA.PropPattern.EQUAL, true))
                            }
                            if (f.classes && f.classes.length > 0) {
                                prop = find(trunc.props, function (p) { return p.name === IIRPA.dom.Attr.CLASS; })
                                addOrReplace(trunc.props, prop, IIRPA.buildProp(IIRPA.dom.Attr.CLASS, f.classes.join(' '), IIRPA.PropPattern.CONTAIN, true))
                            }
                        }
                    });
                }
            };
        };

        return SelectorGenerateor;
    })();

})();
