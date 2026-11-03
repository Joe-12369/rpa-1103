using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.WdtErp
{
    enum BehaviorEvent
    {

        /// BUTTON_CLICK -> 0
        BUTTON_CLICK = 0,

        /// BUTTON_PRESS -> 1
        BUTTON_PRESS = 1,

        /// BUTTON_STATE_CHANGED -> 2
        BUTTON_STATE_CHANGED = 2,

        /// EDIT_VALUE_CHANGING -> 3
        EDIT_VALUE_CHANGING = 3,

        /// EDIT_VALUE_CHANGED -> 4
        EDIT_VALUE_CHANGED = 4,

        /// SELECT_SELECTION_CHANGED -> 5
        SELECT_SELECTION_CHANGED = 5,

        /// SELECT_STATE_CHANGED -> 6
        SELECT_STATE_CHANGED = 6,

        /// POPUP_REQUEST -> 7
        POPUP_REQUEST = 7,

        /// POPUP_READY -> 8
        POPUP_READY = 8,

        /// POPUP_DISMISSED -> 9
        POPUP_DISMISSED = 9,

        /// MENU_ITEM_ACTIVE -> 0xA
        MENU_ITEM_ACTIVE = 10,

        /// MENU_ITEM_CLICK -> 0xB
        MENU_ITEM_CLICK = 11,

        /// CONTEXT_MENU_SETUP -> 0xF
        CONTEXT_MENU_SETUP = 15,

        /// CONTEXT_MENU_REQUEST -> 0x10
        CONTEXT_MENU_REQUEST = 16,

        /// VISIUAL_STATUS_CHANGED -> 0x11
        VISIUAL_STATUS_CHANGED = 17,

        /// DISABLED_STATUS_CHANGED -> 0x12
        DISABLED_STATUS_CHANGED = 18,

        /// POPUP_DISMISSING -> 0x13
        POPUP_DISMISSING = 19,

        /// HYPERLINK_CLICK -> 0x80
        HYPERLINK_CLICK = 128,

        TABLE_HEADER_CLICK,

        TABLE_ROW_CLICK,

        TABLE_ROW_DBL_CLICK,

        /// ELEMENT_COLLAPSED -> 0x90
        ELEMENT_COLLAPSED = 144,

        ELEMENT_EXPANDED,

        ACTIVATE_CHILD,

        /// DO_SWITCH_TAB -> ACTIVATE_CHILD
        DO_SWITCH_TAB = BehaviorEvent.ACTIVATE_CHILD,

        INIT_DATA_VIEW,

        ROWS_DATA_REQUEST,

        UI_STATE_CHANGED,

        FORM_SUBMIT,

        FORM_RESET,

        DOCUMENT_COMPLETE,

        HISTORY_PUSH,

        HISTORY_DROP,

        HISTORY_PRIOR,

        HISTORY_NEXT,

        HISTORY_STATE_CHANGED,

        CLOSE_POPUP,

        REQUEST_TOOLTIP,

        /// ANIMATION -> 0xA0
        ANIMATION = 160,

        /// FIRST_APPLICATION_EVENT_CODE -> 0x100
        FIRST_APPLICATION_EVENT_CODE = 256,
    }

}
