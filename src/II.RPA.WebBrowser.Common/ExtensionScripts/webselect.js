var __defProp = Object.defineProperty;
var __defNormalProp = (obj, key, value) => key in obj ? __defProp(obj, key, { enumerable: true, configurable: true, writable: true, value }) : obj[key] = value;
var __publicField = (obj, key, value) => __defNormalProp(obj, typeof key !== "symbol" ? key + "" : key, value);
(function() {
  "use strict";
  class DropdownStrategyFactory {
    constructor() {
      __publicField(this, "strategies", {});
    }
    registerStrategy(name, strategy) {
      this.strategies[name] = strategy;
    }
    createStrategy(name) {
      if (name == null) {
        return null;
      }
      const Strategy = this.strategies[name];
      return Strategy;
    }
    registerStrategiesFromObject(strategyObject) {
      for (const name in strategyObject) {
        this.registerStrategy(name, strategyObject[name]);
      }
    }
  }
  const domainStrategyMap = {
    "buyertrade.taobao.com": "taobao",
    "item.taobao.com": "taobao-addr",
    "item.jd.com": "jingdong",
    "etax.zhejiang.chinatax.gov.cn": "chinatax",
    "antdv.com": "antdv",
    "element.eleme.cn": "element"
  };
  class BaseDropdownStrategy {
    constructor(dropdownClass = "[role='listbox']", dropdownItemClass = "[role='option']", dropdownItemClickClass = "") {
      __publicField(this, "strategyName", "base");
      __publicField(this, "order", 0);
      __publicField(this, "dropdownClass");
      __publicField(this, "dropdownItemClass");
      __publicField(this, "dropdownItemClickClass");
      this.dropdownClass = dropdownClass;
      this.dropdownItemClass = dropdownItemClass;
      this.dropdownItemClickClass = dropdownItemClickClass;
    }
    validate(element) {
      return [element, element.parentElement, element.parentElement?.parentElement].some(
        (ele) => {
          if (ele?.getAttribute("role") == "combobox") {
            let owns = ele.getAttribute("aria-owns") ?? ele.parentElement?.getAttribute("aria-owns") ?? "";
            console.log("validate owns", owns);
            element.setAttribute("select-owns", ele.getAttribute("aria-owns") ?? ele.parentElement?.getAttribute("aria-owns") ?? "");
            return true;
          }
          return false;
        }
      );
    }
    getValues(element) {
      const dropdown = { items: [], selected: [] };
      let dropdownMenu = element.getDropdownPanel(this.dropdownClass, this.strategyName != "taobao-addr");
      const menuItems = dropdownMenu?.querySelectorAll(this.dropdownItemClass);
      if (menuItems == null) {
        throw new Error(`${dropdownMenu},items not found`);
      }
      menuItems.forEach((item) => {
        const text = item.innerText.trim();
        dropdown.items.push(text);
        if (this.checkSelected(item)) {
          dropdown.selected.push(text);
        }
      });
      return dropdown;
    }
    setValues(element, values) {
      let dropdownMenu = element.getDropdownPanel(this.dropdownClass, this.strategyName != "taobao-addr");
      const menuItems = dropdownMenu?.querySelectorAll(this.dropdownItemClass);
      if (menuItems == null) {
        throw new Error(`${dropdownMenu},items not found`);
      }
      values.forEach((item) => {
        if (item >= 0 && menuItems?.[item]) {
          if (this.dropdownItemClickClass == "") {
            menuItems?.[item].simulateClick();
          } else {
            menuItems?.[item].querySelector(this.dropdownItemClickClass)?.simulateClick();
          }
        }
      });
    }
    checkSelected(element) {
      if (element.getAttribute("aria-selected") == "true") return true;
      if (element.className.indexOf("-selected") > 0) return true;
      if (element.className.indexOf("-active") > 0) return true;
      return false;
    }
  }
  const __vite_glob_0_0 = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
    __proto__: null,
    default: BaseDropdownStrategy
  }, Symbol.toStringTag, { value: "Module" }));
  class BootstrapDropdownStrategy extends BaseDropdownStrategy {
    constructor() {
      super('ul[class*="dropdown-menu"]', "li");
      __publicField(this, "strategyName", "bootstrap");
      __publicField(this, "order", 1);
    }
    validate(element) {
      return [element, element.parentElement, element.parentElement?.parentElement].some(
        (ele) => {
          if (ele?.classList.contains("dropdown-toggle")) {
            if (ele?.parentElement?.id) {
              element.setAttribute("select-owns", ele.parentElement.id);
            } else {
              let _owns = `rpa_${Date.now()}`;
              ele.parentElement?.setAttribute("id", _owns);
              element.setAttribute("select-owns", _owns);
            }
            return true;
          }
          return false;
        }
      );
    }
  }
  const __vite_glob_0_1 = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
    __proto__: null,
    default: BootstrapDropdownStrategy
  }, Symbol.toStringTag, { value: "Module" }));
  class ElementDropdownStrategy extends BaseDropdownStrategy {
    constructor() {
      super('ul[class*="el-select-dropdown__list"]', 'li[class*="el-select-dropdown__item"]');
      __publicField(this, "strategyName", "element");
      __publicField(this, "order", 2);
    }
    validate(element) {
      if (element.classList.contains("el-select")) {
        return true;
      }
      if (element.parentElement?.classList.contains("el-select")) {
        return true;
      }
      if (element.parentElement?.parentElement?.classList.contains("el-select")) {
        return true;
      } else {
        return false;
      }
    }
    checkSelected(element) {
      return element.classList.contains("selected");
    }
  }
  const __vite_glob_0_2 = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
    __proto__: null,
    default: ElementDropdownStrategy
  }, Symbol.toStringTag, { value: "Module" }));
  class JDDropdownStrategy extends BaseDropdownStrategy {
    constructor() {
      super("div[class='ui-switchable-panel ui-switchable-panel-selected'", "li", "a");
      __publicField(this, "strategyName", "jingdong");
      __publicField(this, "order", 4);
    }
    validate(element) {
      if (/^ui-switchable-item/.test(element.className)) {
        return true;
      }
      if (/^ui-switchable-item/.test(element.parentElement?.className ?? "")) {
        return true;
      } else {
        return false;
      }
    }
    checkSelected(element) {
      return element.className == "ui-area-current";
    }
  }
  const __vite_glob_0_3 = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
    __proto__: null,
    default: JDDropdownStrategy
  }, Symbol.toStringTag, { value: "Module" }));
  class TaobaoAddressDropdownStrategy extends BaseDropdownStrategy {
    constructor() {
      super('div[class^="AddressDialog"]', 'div[class^="UserAddress--userAddressItem"]', "div[class^='UserAddress--detailAddress']");
      __publicField(this, "strategyName", "taobao-addr");
      __publicField(this, "order", 3);
    }
    validate(element) {
      if (/^Address--select/.test(element.className)) {
        return true;
      }
      if (/^Address--select/.test(element.parentElement ? element.parentElement.className : "")) {
        return true;
      } else {
        return false;
      }
    }
    checkSelected(element) {
      if (element.className.indexOf("UserAddress--isSelected") != -1) {
        return true;
      }
      return false;
    }
  }
  const __vite_glob_0_4 = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
    __proto__: null,
    default: TaobaoAddressDropdownStrategy
  }, Symbol.toStringTag, { value: "Module" }));
  class TaobaoDropdownStrategy extends BaseDropdownStrategy {
    constructor() {
      super('ul[role="menu"]', "li[role='menuitem']");
      __publicField(this, "strategyName", "taobao");
      __publicField(this, "order", 2);
    }
    validate(element) {
      if (element.getAttribute("role") === "combobox") {
        return true;
      }
      if (element.parentElement?.getAttribute("role") === "combobox") {
        return true;
      } else {
        return false;
      }
    }
    checkSelected(element) {
      if (element.getAttribute("aria-selected") === "true") {
        return true;
      }
      return false;
    }
  }
  const __vite_glob_0_5 = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
    __proto__: null,
    default: TaobaoDropdownStrategy
  }, Symbol.toStringTag, { value: "Module" }));
  class TaxDropdownStrategy extends BaseDropdownStrategy {
    constructor() {
      super('ul[class^="dropdown-menu"]', 'a[role="option"]');
      __publicField(this, "strategyName", "chinatax");
      __publicField(this, "order", 5);
    }
    validate(element) {
      if (element.getAttribute("data-toggle") == "dropdown") {
        return true;
      }
      if (element.parentElement?.getAttribute("data-toggle") == "dropdown") {
        return true;
      } else {
        return false;
      }
    }
  }
  const __vite_glob_0_6 = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
    __proto__: null,
    default: TaxDropdownStrategy
  }, Symbol.toStringTag, { value: "Module" }));
  class VueAntDropdownStrategy extends BaseDropdownStrategy {
    constructor() {
      super('div[class*="ant-select-dropdown"]', 'div[class*="ant-select-item ant-select-item-option"]', 'div[class*="ant-select-item-option-content"]');
      __publicField(this, "strategyName", "antdv");
      __publicField(this, "order", 1);
    }
    validate(element) {
      if (element.classList.contains("ant-select-selector")) {
        return true;
      }
      if (element.parentElement?.classList.contains("ant-select-selector")) {
        return true;
      } else {
        return false;
      }
    }
  }
  const __vite_glob_0_7 = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
    __proto__: null,
    default: VueAntDropdownStrategy
  }, Symbol.toStringTag, { value: "Module" }));
  function loadStrategies() {
    const strategies = {};
    const modules = /* @__PURE__ */ Object.assign({ "./strategies/BaseDropdownStrategy.ts": __vite_glob_0_0, "./strategies/BootstrapDropdownStrategy.ts": __vite_glob_0_1, "./strategies/ElementDropdownStrategy.ts": __vite_glob_0_2, "./strategies/JDDropdownStrategy.ts": __vite_glob_0_3, "./strategies/TaobaoAddressDropdownStrategy.ts": __vite_glob_0_4, "./strategies/TaobaoDropdownStrategy.ts": __vite_glob_0_5, "./strategies/TaxDropdownStrategy.ts": __vite_glob_0_6, "./strategies/VueAntDropdownStrategy.ts": __vite_glob_0_7 });
    for (const path in modules) {
      const strategyModule = modules[path];
      const strategyClass = new strategyModule.default();
      strategies[strategyClass.strategyName] = strategyClass;
    }
    return strategies;
  }
  class DropdownStrategyManager {
    constructor() {
      __publicField(this, "strategyFactory");
      __publicField(this, "domainStrategyMap");
      __publicField(this, "strategies", {});
      this.strategyFactory = new DropdownStrategyFactory();
      this.domainStrategyMap = domainStrategyMap;
      this.strategies = loadStrategies();
      this.registerStrategies(this.strategies);
    }
    registerStrategies(strategies) {
      this.strategyFactory.registerStrategiesFromObject(strategies);
    }
    getStrategyForDomain(domain) {
      return this.domainStrategyMap[domain] || null;
    }
    getCurrentDomain() {
      return window.location.hostname;
    }
    findStrategy(element, strategyName) {
      if (!strategyName) {
        const domain = this.getCurrentDomain();
        strategyName = this.getStrategyForDomain(domain);
      }
      let strategy = this.strategyFactory.createStrategy(strategyName);
      if (strategy && strategy.validate(element)) {
        return strategy;
      }
      for (const item in this.strategies) {
        if (strategyName === item) {
          continue;
        }
        strategy = this.strategyFactory.createStrategy(item);
        if (strategy?.validate(element)) {
          return strategy;
        }
      }
      return null;
    }
    validateDropdown(element, click = false) {
      let strategy = this.findStrategy(element, null);
      if (strategy != null && strategy.validate(element)) {
        console.log("find dropdown strategy:", strategy?.strategyName);
        return { success: true, result: strategy.strategyName };
      } else {
        return { success: false, message: "dropdown element not found" };
      }
    }
    getDropdownValues(element, strategyName) {
      console.debug("%get dropdown values: " + Date(), "color:red;font-size:18px");
      try {
        const strategy = this.findStrategy(element, strategyName);
        if (strategy == null) {
          return { success: false, message: `${strategyName} strategy validation failed` };
        }
        const values = strategy.getValues(element);
        return { success: true, result: values };
      } catch (error) {
        console.error("get dropdown values error:", error);
        element.simulateClick();
        return {
          success: false,
          message: error instanceof Error ? error.message : "Unknown error"
        };
      }
    }
    setDropdownValues(element, strategyName, values) {
      console.debug("%cset dropdown values: " + Date(), "color:red;font-size:18px");
      try {
        const strategy = this.findStrategy(element, strategyName);
        if (strategy == null) {
          return { success: false, message: `${strategyName} validation failed` };
        }
        strategy.setValues(element, values);
        return { success: true };
      } catch (error) {
        console.error("set dropdown values error:", error);
        element.simulateClick();
        return {
          success: false,
          message: error instanceof Error ? error.message : "Unknown error"
        };
      }
    }
  }
  const strategyManager = new DropdownStrategyManager();
  function validate(element, click = false) {
    return strategyManager.validateDropdown(element, click);
  }
  function getValues(element, strategyName = null) {
    let result = strategyManager.getDropdownValues(element, strategyName);
    if (result && result.success && result.result && result.result.items && result.result.items.length > 0) {
      setTimeout(() => {
        try {
          if (lastSimulatedDropdown && lastSimulatedDropdown.isConnected) {
            ["mousedown", "mouseup", "click"].forEach((type) => {
              var event = new MouseEvent(type, {
                "view": window,
                "bubbles": true,
                "cancelable": true
              });
              lastSimulatedDropdown.dispatchEvent(event);
            });
          }
        } catch (e) {
        }
      }, 100);
    }
    return result;
  }
  function setValues(element, values, strategyName = null) {
    return strategyManager.setDropdownValues(element, strategyName, values);
  }
  const globalWindow = window;
  globalWindow.webselect = {
    validate,
    getValues,
    setValues
  };
  let lastSimulatedDropdown = null;
  globalWindow.HTMLElement.prototype.simulateClick = function() {
    const dropdown = this;
    if (lastSimulatedDropdown && lastSimulatedDropdown !== dropdown && lastSimulatedDropdown.isConnected) {
      try {
        ["mousedown", "mouseup", "click"].forEach((type) => {
          var event = new MouseEvent(type, { "view": window, "bubbles": true, "cancelable": true });
          lastSimulatedDropdown.dispatchEvent(event);
        });
      } catch (e) {
      }
    }
    lastSimulatedDropdown = dropdown;
    ["mousedown", "mouseup", "click"].forEach((type) => {
      var event = new MouseEvent(type, {
        "view": window,
        "bubbles": true,
        "cancelable": true
      });
      dropdown.dispatchEvent(event);
    });
  };
  globalWindow.HTMLElement.prototype.getDropdownPanel = function(targetSelector, validatePosition = true) {
    const element = this;
    let elements = element.ownerDocument.querySelectorAll(targetSelector);
    var owns = element.getAttribute("select-owns");
    if (owns) {
      let ownsPanel = element.ownerDocument.getElementById(owns);
      console.debug("dropdown panel owns:", ownsPanel);
      if (ownsPanel) {
        if (ownsPanel.matches(targetSelector)) {
          return ownsPanel;
        } else {
          elements = ownsPanel.querySelectorAll(targetSelector);
        }
      } else {
        console.debug(`No visible element found for owns: ${owns}`);
        return null;
      }
    }
    console.debug("find dropdown panel length:", elements.length);
    let bestPanel = null;
    let minDistance = Infinity;
    for (var i = 0; i < elements.length; i++) {
      let panel = elements[i];
      if (panel.isVisible()) {
        if (!validatePosition) return panel;
        let elementRect = element.getBoundingClientRect();
        let panelRect = panel.getBoundingClientRect();
        let dx = Math.max(0, elementRect.left - panelRect.right, panelRect.left - elementRect.right);
        let verticalGap = Math.min(
          Math.abs(panelRect.top - elementRect.bottom),
          // 正常向下展开
          Math.abs(panelRect.bottom - elementRect.top)
          // 向上展开
        );
        let distance = Math.sqrt(dx * dx + verticalGap * verticalGap);
        console.debug(`panel [${i}] bounding rect:`, panelRect, "distance:", distance, "dx:", dx, "verticalGap:", verticalGap, "html block:", panel.outerHTML.substring(0, 150));
        if (dx > 50 || verticalGap > 80) {
          console.debug(`panel [${i}] is not edge-aligned (dx:${dx}, gap:${verticalGap}), ignoring.`);
          continue;
        }
        if (distance < minDistance) {
          minDistance = distance;
          bestPanel = panel;
        }
      }
    }
    if (bestPanel) {
      console.debug("selected best panel distance:", minDistance, bestPanel);
      return bestPanel;
    }
    console.debug(`No visible element found for selector: ${targetSelector}`);
    return null;
  };
  globalWindow.HTMLElement.prototype.isVisible = function() {
    if (!this) return false;
    const rect = this.getBoundingClientRect();
    if (rect.width > 0 && rect.height > 0) {
      const style = getComputedStyle(this);
      if (style.visibility === "hidden" || style.opacity === "0") {
        return false;
      }
      return true;
    }
    return false;
  };
})();
//# sourceMappingURL=webselect.js.map
