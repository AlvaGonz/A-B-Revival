package ui.pages
{
   import f4.data.InitData;
   import flash.display.DisplayObjectContainer;
   import flash.display.GradientType;
   import flash.display.Graphics;
   import flash.display.Sprite;
   import flash.events.MouseEvent;
   import flash.filters.GlowFilter;
   import flash.geom.Matrix;
   import flash.text.TextField;
   import flash.text.TextFieldAutoSize;
   import flash.text.TextFormat;
   import flash.text.TextFormatAlign;
   import ui.base.UIStyles;

   /**
    * In-game modal dialog displaying all controls and keybindings
    * grouped into Combat, Assembly (Factory), and Fleet & Economy.
    */
   public class UIControlsModal extends Sprite
   {
      private var _parentContainer:DisplayObjectContainer;
      private var _backdrop:Sprite;
      private var _panel:Sprite;
      private var _btnClose:Sprite;
      private var _txtClose:TextField;
      private var _isCloseHovered:Boolean = false;

      public function UIControlsModal(param1:DisplayObjectContainer)
      {
         super();
         this._parentContainer = param1;
         this.buildModal();
         this._parentContainer.addChild(this);
      }

      private function buildModal() : void
      {
         this._backdrop = new Sprite();
         var bgG:Graphics = this._backdrop.graphics;
         bgG.beginFill(0x000000, 0.78);
         bgG.drawRect(0, 0, 640, 480);
         bgG.endFill();
         this._backdrop.addEventListener(MouseEvent.CLICK, this.onBackdropClick);
         this.addChild(this._backdrop);

         this._panel = new Sprite();
         this._panel.x = 40;
         this._panel.y = 35;
         var pG:Graphics = this._panel.graphics;
         pG.beginFill(0x0A0E18, 0.96);
         var mat:Matrix = new Matrix();
         mat.createGradientBox(560, 410, 0, 0, 0);
         pG.lineStyle(1.5);
         pG.lineGradientStyle(GradientType.LINEAR, [0x00CCFF, 0x336699, 0x00CCFF], [0.8, 0.4, 0.8], [0, 128, 255], mat);
         
         // Chamfered container box
         pG.moveTo(15, 0);
         pG.lineTo(545, 0);
         pG.lineTo(560, 15);
         pG.lineTo(560, 395);
         pG.lineTo(545, 410);
         pG.lineTo(15, 410);
         pG.lineTo(0, 395);
         pG.lineTo(0, 15);
         pG.lineTo(15, 0);
         pG.endFill();

         // Header title
         var titleTxt:TextField = new TextField();
         titleTxt.width = 560;
         titleTxt.height = 28;
         titleTxt.y = 12;
         titleTxt.selectable = false;
         titleTxt.mouseEnabled = false;
         UIStyles.applyFormat(titleTxt, UIStyles.FONT_TITLE, UIStyles.SIZE_HEADER, UIStyles.COLOR_SYSTEM_LIGHT, true, TextFormatAlign.CENTER);
         UIStyles.applyHoloGlow(titleTxt, UIStyles.COLOR_SYSTEM_CYAN);
         titleTxt.text = "CONTROLS & KEYBINDS REFERENCE";
         this._panel.addChild(titleTxt);

         // Subtitle divider
         var divG:Graphics = this._panel.graphics;
         divG.lineStyle(1, 0x335577, 0.6);
         divG.moveTo(20, 44);
         divG.lineTo(540, 44);

         // Group 1: Combat (Left Column)
         var col1X:Number = 18;
         var col1W:Number = 170;
         this.createGroupHeader("COMBAT", col1X, 52, col1W);
         var combatRows:Array = [
            ["MOUSE", "Aim & Flight Heading"],
            ["L-CLICK", "Fire Active Weapons"],
            ["SPACE", "Engine Boost (Thrust)"],
            ["X", "Toggle Autofire Reticle"],
            ["C", "Cycle Camera Zoom FOV"],
            ["E", "Cycle Captain AI Mode"],
            ["B", "Instant Mission Win (Debug)"]
         ];
         this.renderKeyGroup(combatRows, col1X, 74, col1W);

         // Group 2: Assembly / Factory (Center Column)
         var col2X:Number = 195;
         var col2W:Number = 170;
         this.createGroupHeader("SHIPBUILDER", col2X, 52, col2W);
         var factoryRows:Array = [
            ["DRAG/DROP", "Mount & Move Parts"],
            ["R KEY", "Rotate Wing (Clockwise)"],
            ["F KEY", "Flip Wing Horizontally"],
            ["CLR GUNS", "Strip Weapons to Inv."],
            ["CLR WINGS", "Strip Wings to Inv."],
            ["STATS", "Live Hull Wing Limits"],
            ["BODY", "Single Hull Stock Policy"]
         ];
         this.renderKeyGroup(factoryRows, col2X, 74, col2W);

         // Group 3: Fleet & Options (Right Column)
         var col3X:Number = 372;
         var col3W:Number = 170;
         this.createGroupHeader("FLEET & SYSTEM", col3X, 52, col3W);
         var systemRows:Array = [
            ["L-CLICK", "Buy / Sell 1 Item"],
            ["HOLD CLICK", "Fast Purchase Stacking"],
            ["AI CHOOSER", "Squad AI Fleet Setup"],
            ["FLAGSHIP", "Captain Craft Protected"],
            ["AUTOSAVE", "Auto-Save on Victory"],
            ["DISPLAY", "640 / 720 / 1080 / Full"],
            ["ESC / BACK", "Exit to Menu / Map"]
         ];
         this.renderKeyGroup(systemRows, col3X, 74, col3W);

         // Close button
         this._btnClose = new Sprite();
         this._btnClose.buttonMode = true;
         this._btnClose.useHandCursor = true;
         this._btnClose.x = 280;
         this._btnClose.y = 372;

         this._txtClose = new TextField();
         this._txtClose.width = 120;
         this._txtClose.height = 20;
         this._txtClose.x = -60;
         this._txtClose.y = -9;
         this._txtClose.selectable = false;
         this._txtClose.mouseEnabled = false;
         UIStyles.applyFormat(this._txtClose, UIStyles.FONT_BODY, 11, UIStyles.COLOR_WHITE, true, TextFormatAlign.CENTER);
         UIStyles.applyGlow(this._txtClose, 0x000000, 0.8, 3, 2);
         this._txtClose.text = "[ CLOSE ]";

         this._btnClose.addChild(this._txtClose);
         this._btnClose.addEventListener(MouseEvent.MOUSE_OVER, this.onCloseOver);
         this._btnClose.addEventListener(MouseEvent.MOUSE_OUT, this.onCloseOut);
         this._btnClose.addEventListener(MouseEvent.CLICK, this.onCloseClick);

         this._panel.addChild(this._btnClose);
         this.updateCloseBtn();

         this.addChild(this._panel);
      }

      private function createGroupHeader(param1:String, param2:Number, param3:Number, param4:Number) : void
      {
         var hTxt:TextField = new TextField();
         hTxt.x = param2;
         hTxt.y = param3;
         hTxt.width = param4;
         hTxt.height = 20;
         hTxt.selectable = false;
         hTxt.mouseEnabled = false;
         UIStyles.applyFormat(hTxt, UIStyles.FONT_HEADER, UIStyles.SIZE_HUD, UIStyles.COLOR_CAPTAIN_AMBER, true, TextFormatAlign.CENTER);
         UIStyles.applyHoloGlow(hTxt, UIStyles.COLOR_CAPTAIN_AMBER);
         hTxt.text = param1;
         this._panel.addChild(hTxt);
      }

      private function renderKeyGroup(param1:Array, param2:Number, param3:Number, param4:Number) : void
      {
         var rowY:Number = param3;
         for each(var item:Array in param1)
         {
            var keyTxt:TextField = new TextField();
            keyTxt.x = param2;
            keyTxt.y = rowY;
            keyTxt.width = param4 * 0.42;
            keyTxt.height = 36;
            keyTxt.selectable = false;
            keyTxt.mouseEnabled = false;
            keyTxt.wordWrap = true;
            UIStyles.applyFormat(keyTxt, UIStyles.FONT_BODY, UIStyles.SIZE_SMALL, UIStyles.COLOR_SYSTEM_LIGHT, true, TextFormatAlign.RIGHT);
            UIStyles.applyGlow(keyTxt, 0x000000, 0.8, 2, 2);
            keyTxt.text = item[0];
            this._panel.addChild(keyTxt);

            var descTxt:TextField = new TextField();
            descTxt.x = param2 + param4 * 0.45;
            descTxt.y = rowY;
            descTxt.width = param4 * 0.55;
            descTxt.height = 36;
            descTxt.selectable = false;
            descTxt.mouseEnabled = false;
            descTxt.wordWrap = true;
            UIStyles.applyFormat(descTxt, UIStyles.FONT_BODY, 9, UIStyles.COLOR_SILVER, false, TextFormatAlign.LEFT);
            UIStyles.applyGlow(descTxt, 0x000000, 0.8, 2, 2);
            descTxt.text = item[1];
            this._panel.addChild(descTxt);

            rowY += 38;
         }
      }

      private function updateCloseBtn() : void
      {
         var g:Graphics = this._btnClose.graphics;
         g.clear();
         g.beginFill(0x101826, 0.95);
         if(this._isCloseHovered)
         {
            g.lineStyle(1.2, 0x95E0F2, 1.0);
         }
         else
         {
            var mat:Matrix = new Matrix();
            mat.createGradientBox(120, 22, 0, -60, -11);
            g.lineStyle(1.0);
            g.lineGradientStyle(GradientType.LINEAR, [0xFFFFFF, 0xABBFDC, 0xFFFFFF], [0.2, 0.6, 0.2], [0, 128, 255], mat);
         }
         g.moveTo(54, -11);
         g.lineTo(60, -5);
         g.lineTo(60, 5);
         g.lineTo(54, 11);
         g.lineTo(-54, 11);
         g.lineTo(-60, 5);
         g.lineTo(-60, -5);
         g.lineTo(-54, -11);
         g.lineTo(54, -11);
         g.endFill();

         if(this._txtClose)
         {
            this._txtClose.textColor = this._isCloseHovered ? 0xFFFFFF : 0xBFD7EA;
         }
      }

      private function onCloseOver(e:MouseEvent) : void
      {
         this._isCloseHovered = true;
         this.updateCloseBtn();
      }

      private function onCloseOut(e:MouseEvent) : void
      {
         this._isCloseHovered = false;
         this.updateCloseBtn();
      }

      private function onCloseClick(e:MouseEvent) : void
      {
         this.close();
      }

      private function onBackdropClick(e:MouseEvent) : void
      {
         if(e.target == this._backdrop)
         {
            this.close();
         }
      }

      public function close() : void
      {
         if(InitData.SOUNDC2 && InitData.SOUNDDATA && InitData.SOUNDDATA.soundArray && InitData.SOUNDDATA.soundArray.length > 18)
         {
            InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[18], 1);
         }
         if(this._btnClose)
         {
            this._btnClose.removeEventListener(MouseEvent.MOUSE_OVER, this.onCloseOver);
            this._btnClose.removeEventListener(MouseEvent.MOUSE_OUT, this.onCloseOut);
            this._btnClose.removeEventListener(MouseEvent.CLICK, this.onCloseClick);
         }
         if(this._backdrop)
         {
            this._backdrop.removeEventListener(MouseEvent.CLICK, this.onBackdropClick);
         }
         if(this.parent)
         {
            this.parent.removeChild(this);
         }
      }
   }
}
