//@ts-nocheck
var UNIQUE_KEY = "i-rpa";

function IsCovered(uniqueId) {
    var element = elementFromUniqueId(uniqueId);
    var bounding = element.getBoundingClientRect();

    if (bounding.left < 0 || bounding.top < 0) {
        return true;
    }
    var x = bounding.left,
        y = bounding.top,
        width = bounding.width,
        height = bounding.height
    var pointElement = IIRPA.dom.elementFromPoint(x + width / 2, y + height / 2);
    return pointElement !== element && !element.contains(pointElement);
}
function initEvent(uniqueId, eventName) {
    try {
        var element = elementFromUniqueId(uniqueId)
        var event = document.createEvent('Events');
        event.initEvent(eventName, true, false);
        element.dispatchEvent(event);
        return true;
    } catch (e) {
        console.log(e);
        return false;
    }
}
function Scroll(uniqueId, direction, scrollType, targetuniqueId, offSetX, offSetY) {
    var node;
    var targetNode;
    if (uniqueId) {
        node = elementFromUniqueId(uniqueId);
        if (!node) {
            return {
                'status': IIRPA.ErrorCode.NO_SUCH_ELEMENT,
                'value': '未找到控件'
            };
        }
    }
    if (targetuniqueId) {
        targetNode = elementFromUniqueId(targetuniqueId);
        if (!targetNode) {
            return {
                'status': IIRPA.ErrorCode.NO_SUCH_ELEMENT,
                'value': '未找到控件'
            };
        }
    }
    IIRPA.api.scroll_v2(node, direction, scrollType, targetNode, offSetX, offSetY);
}
function GetScroll(direction) {
    return IIRPA.api.getScroll("", direction).value;
}
function GetScroll_V2(uniqueId, direction) {
    if (uniqueId === "") {
        return IIRPA.api.getScroll(null, direction).value;
    }
    else {
        var ele = elementFromUniqueId(uniqueId);
        return IIRPA.api.getScroll(ele, direction).value;
    }


}
function ScrollIntoView_V2(uniqueId, location) {
    var ele = elementFromUniqueId(uniqueId);
    if (ele) {
        if (ele.scrollIntoView)
            ele.scrollIntoView();
    }
}
function ScrollIntoView(uniqueId, location, frameLeft, frameTop) {
    var ele = elementFromUniqueId(uniqueId);
    if (ele) {
        if (!IIRPA.api.visibleLocation(ele, location, frameLeft, frameTop).value) {
            if (ele.scrollIntoView) {
                ele.scrollIntoView();
            }
        }
    }
}
function GetElement(index) {
    if (elements) {
        return elements[index];
    }
    return null;
}

function getIsTableName(uniqueId) {
    // params.elementId      
    var ele = elementFromUniqueId(uniqueId);
    if (ele) {
        var result = IIRPA.api.getIsTableName(ele).value;
        if (result && result.isTable === true) {
            return result.element;
        }
    }
    return null;
}
function getTableData(uniqueId) {
    var ele = elementFromUniqueId(uniqueId);
    return IIRPA.api.tableData(ele, "text").value;
}
function getNextElementBuildSelector(cssSelector, innerTextFirst) {
    var ele = IIRPA.api.getNextElement(cssSelector);
    var elePath = IIRPA.JSON().parse(IIRPA.api.inspectPath(ele, { innerTextFirst: innerTextFirst == 'true' }).value)
    return IIRPA.JSON().stringify(elePath);
}
function getTurningPageElement(cssSelectors) {
    var ele = IIRPA.CollectionUtils.getTurningPageElement(cssSelectors);
    return ele;
}

var elements = [];
function QuerySelector(selector, cssSelector, version, parentId, discernHidden) {
    try {
        //if (IIRPA.ieVersion < 8)
        //    return 0;
        elements = [];
        var parent = null
        if (parentId != "" && parentId != null) {
            parent = elementFromUniqueId(parentId)
        }
        if (cssSelector != "") {
            try {
                var context = parent || document;
                elements = context.querySelectorAll(cssSelector);
            } catch (e) {

            }
        }
        if (elements.length == 0) {
            var context = parent || document;
            elements = IIRPA.locale.findElements(selector, context, version, discernHidden);
        }
        return elements.length;
    } catch (e) {
        return 0;
    }
}
//批量返回元素的uniqueId集合字符串
var uniqueIds = '';
function SetUniqueIdArray(frameArry, index) {
    try {
        //if (IIRPA.ieVersion < 8)
        //    return 0;
        uniqueIds = '';
        //uniqueValues = elements.length.toString();
        if (window._iirpa_recent_id == null) {
            window._iirpa_recent_id = 1
        }
        for (var i = 0; i < elements.length; i++) {
            //设置具体索引值对应的元素的Id
            if (index != -1) {
                if (index != i) {
                    continue;
                }
            }
            var uniqueId = elements[i].getAttribute(UNIQUE_KEY);
            if (uniqueId != null) {
                uniqueIds = uniqueIds + ',' + uniqueId + '/' + elements[i].tagName;
            }
            else {
                window._iirpa_recent_id += 1;
                var uniqueValue = frameArry + '/' + window._iirpa_recent_id;
                elements[i].setAttribute(UNIQUE_KEY, uniqueValue);
                uniqueIds = uniqueIds + ',' + uniqueValue + '/' + elements[i].tagName;
            }
        }
        if (uniqueIds != '') {
            uniqueIds = uniqueIds.substring(1);
        }
        return uniqueIds;
    } catch (e) {
        return 0;
    }
}
function buildCssSelector(uniqueId) {
    if (uniqueId) {
        var element = elementFromUniqueId(uniqueId);
        var cssSelector = IIRPA.api.inspectcssSelector(element).value;
        return cssSelector.path;
    } else {
        var frameElement = getChildFrameByIndex(index);
        if (frameElement) {
            var framePath = IIRPA.api.inspectcssSelector(element).value;
            return framePath;
        }
    }
}
function buildSelector(uniqueId, index, innerTextFirst, rawData, excludeInnerText, excludeHiddenEle, validNodeAttributes) {
    var selector = "";
    //if (IIRPA.ieVersion < 8)
    //    return "";
    var _validNodeAttributes = !validNodeAttributes ? null : IIRPA.JSON().parse(validNodeAttributes)
    var options = {
        innerTextFirst: innerTextFirst,
        rawData: rawData,
        excludeInnerText: excludeInnerText,
        excludeHiddenEle: excludeHiddenEle,
        validNodeAttributes: _validNodeAttributes,
    }
    if (uniqueId) {
        var element = elementFromUniqueId(uniqueId)
        var selector = IIRPA.api.inspectPath(element, options).value
        return selector
    } else {
        var frameElement = getChildFrameByIndex(index)
        if (frameElement) {
            var framePath = IIRPA.api.inspectPath(frameElement, options).value
            return framePath
        }
    }
}
function getChildFrameByIndex(index) {
    var frameElement = document.getElementsByTagName('iframe')
    for (var i = 0; i < frameElement.length; i++) {
        if (frameElement[i].contentWindow === window.frames[index]) {
            return frameElement[i]
        }
    }
    frameElement = document.getElementsByTagName('frame')
    for (var i = 0; i < frameElement.length; i++) {
        if (frameElement[i].contentWindow === window.frames[index]) {
            return frameElement[i]
        }
    }
    return null
}
function elementFromUniqueId(uniqueId) {
    try {
        var tokens = uniqueId.split('/')
        if (tokens.length < 3)
            console.log("错误")
        var tagName = tokens.pop()
        var uniqueValue = tokens.join('/')
        return IIRPA.dom.querySelectorAll(tagName + "[" + UNIQUE_KEY + "='" + uniqueValue + "']", document)[0]
    } catch (e) {
        console.log(e);
    }
}
//构建selector
function buildBatchSelector(elementIds, index, resultAttrName) {
    try {
        //if (IIRPA.ieVersion < 8)
        //    return null;
        var selectors = '';
        var arraryIds = elementIds.split(',');
        if (resultAttrName == null || resultAttrName == '') {
            for (var i = 0; i < arraryIds.length; i++) {
                var singleSelector = buildSelector(arraryIds[i], index);
                if (i == 0) {
                    selectors = singleSelector;
                }
                else {
                    selectors = selectors + '&' + singleSelector;
                }
            }
        }
        else {
            for (var i = 0; i < arraryIds.length; i++) {
                var singleElement = elementFromUniqueId(arraryIds[i]);
                var attrValue = '';
                if (resultAttrName == 'innerText') {
                    attrValue = singleElement.innerText;
                }
                if (resultAttrName == 'href') {
                    attrValue = singleElement.href;

                }
                if (resultAttrName == 'src') {
                    attrValue = singleElement.src;
                }
                if (i == 0) {
                    if (typeof (attrValue) == "undefined") {
                        selectors = '';
                    }
                    else {
                        selectors = attrValue;
                    }
                }
                else {
                    if (typeof (attrValue) == "undefined") {
                        selectors = selectors + '&' + '';
                    }
                    else {
                        selectors = selectors + '&' + attrValue;
                    }

                }
            }
        }
        return selectors;
    } catch (e) {
        return null;
    }
}

function isDisplayed(uniqueId) {
    var node = elementFromUniqueId(uniqueId)
    if (!node) {
        throw new IIRPA.Error('未找到元素')
    }
    return !IIRPA.dom.IsElementHidden(node, true);
}
function getAndScrollLastElement(selector, selectorVersion, scrollType) {
    var lastElement = IIRPA.CollectionUtils.getAndScrollLastElement(selector, selectorVersion, scrollType, [null]);
    return lastElement;
}
function queryCollectionDataV2(params) {
    var collectionData = IIRPA.CollectionUtils.queryCollectionData(params);
    return IIRPA.JSON().stringify(collectionData);
}
function getUsableAttributes(uniqueId) {
    var ele = elementFromUniqueId(uniqueId);
    if (!ele) {
        throw new IIRPA.Error('未找到元素')
    }
    return IIRPA.JSON().stringify(IIRPA.CollectionUtils.getUsableAttributes(ele));
}

function getSimilarElement(uniqueId) {
    var ele = elementFromUniqueId(uniqueId);
    if (!ele) {
        throw new IIRPA.Error('未找到元素')
    }
    return IIRPA.CollectionUtils.getSimilarElement(ele, "1.1");
}

function scrollToTop(uniqueId, top) {
    var ele;
    if (uniqueId) {
        ele = elementFromUniqueId(uniqueId);
        if (!ele) {
            throw new IIRPA.Error('未找到元素')
        }
    }
    IIRPA.dom.scrollTop(ele, top)
}
function getAttribute_V2(uniqueId, attrName, isIntact) {
    var ele;
    if (uniqueId) {
        ele = elementFromUniqueId(uniqueId);
        if (!ele) {
            throw new IIRPA.Error('未找到元素')
        }
    }
    return IIRPA.JSON().stringify(IIRPA.api.getAttributeValue(ele, attrName, isIntact == 1));
}