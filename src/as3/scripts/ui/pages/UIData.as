package ui.pages
{
   import f4data.sdata.ConfigAndStory;
   import flash.display.DisplayObjectContainer;
   import flash.display.Graphics;
   import flash.display.MovieClip;
   import flash.display.Sprite;
   import flash.events.Event;
   import flash.events.MouseEvent;
   import flash.filters.GlowFilter;
   import flash.net.FileReference;
   import flash.text.TextField;
   import flash.text.TextFormat;
   import flash.text.TextFormatAlign;
   import fullshit.tool.Attach;
   import fullshit.tool.SaveLoad;
   import mochi.as3.MochiEvents;
   import ui.UICtrl;
   import ui.base.BtnBase;
   import ui.base.UIBase;
   
   public class UIData extends UIBase
   {
      
      public var sl:SaveLoad;
      
      public var btnAutosave:Sprite;
      
      public var txtAutosave:TextField;
      
      public var isHovered:Boolean = false;
      
      public function UIData(param1:DisplayObjectContainer, param2:MovieClip, param3:String, param4:UICtrl, param5:UIBase = null)
      {
         this.createAutosaveBtn(param2);
         super(param1,param2,param3,param4,param5);
         this.sl = new SaveLoad("anb",this.loaded);
      }
      
      private function createAutosaveBtn(param1:MovieClip) : void
      {
         this.btnAutosave = new Sprite();
         this.btnAutosave.buttonMode = true;
         this.btnAutosave.useHandCursor = true;
         this.btnAutosave.x = 321;
         this.btnAutosave.y = 282;
         
         this.txtAutosave = new TextField();
         this.txtAutosave.width = 132;
         this.txtAutosave.height = 20;
         this.txtAutosave.x = -66;
         this.txtAutosave.y = -10;
         this.txtAutosave.selectable = false;
         this.txtAutosave.mouseEnabled = false;
         var tf:TextFormat = new TextFormat("Arial", 11, 0xffffff, true);
         tf.align = TextFormatAlign.CENTER;
         this.txtAutosave.defaultTextFormat = tf;
         this.txtAutosave.filters = [new GlowFilter(0x000000, 0.8, 3, 3, 2, 1)];
         
         this.btnAutosave.addChild(this.txtAutosave);
         this.updateAutosaveUI();
         
         this.btnAutosave.addEventListener(MouseEvent.CLICK, this.onAutosaveClick);
         this.btnAutosave.addEventListener(MouseEvent.ROLL_OVER, this.onAutosaveOver);
         this.btnAutosave.addEventListener(MouseEvent.ROLL_OUT, this.onAutosaveOut);
         
         param1.addChild(this.btnAutosave);
      }
      
      public function updateAutosaveUI() : void
      {
         if(!this.btnAutosave)
         {
            return;
         }
         var g:Graphics = this.btnAutosave.graphics;
         g.clear();
         
         var isOn:Boolean = ConfigAndStory.AUTOSAVE;
         var bgColor:uint = this.isHovered ? 0x223348 : 0x141c26;
         var borderColor:uint = 0;
         var textColor:uint = 0;
         
         if(isOn)
         {
            borderColor = this.isHovered ? 0x55ffaa : 0x22cc77;
            textColor = this.isHovered ? 0x88ffcc : 0x44ff88;
         }
         else
         {
            borderColor = this.isHovered ? 0xff7777 : 0x884444;
            textColor = this.isHovered ? 0xffaaaa : 0xcc6666;
         }
         
         g.lineStyle(1.5, borderColor, 0.95);
         g.beginFill(bgColor, 0.92);
         g.drawRoundRect(-66, -12, 132, 24, 6, 6);
         g.endFill();
         
         if(this.txtAutosave)
         {
            this.txtAutosave.textColor = textColor;
            this.txtAutosave.text = isOn ? "AUTOSAVE: ON" : "AUTOSAVE: OFF";
         }
      }
      
      private function onAutosaveClick(e:MouseEvent) : void
      {
         ConfigAndStory.AUTOSAVE = !ConfigAndStory.AUTOSAVE;
         ConfigAndStory.save();
         this.updateAutosaveUI();
      }
      
      private function onAutosaveOver(e:MouseEvent) : void
      {
         this.isHovered = true;
         this.updateAutosaveUI();
      }
      
      private function onAutosaveOut(e:MouseEvent) : void
      {
         this.isHovered = false;
         this.updateAutosaveUI();
      }
      
      override public function removeSelf() : UIBase
      {
         if(this.btnAutosave)
         {
            this.btnAutosave.removeEventListener(MouseEvent.CLICK, this.onAutosaveClick);
            this.btnAutosave.removeEventListener(MouseEvent.ROLL_OVER, this.onAutosaveOver);
            this.btnAutosave.removeEventListener(MouseEvent.ROLL_OUT, this.onAutosaveOut);
         }
         return super.removeSelf();
      }
      
      private function loaded(param1:Event) : void
      {
         var _loc2_:Boolean = ConfigAndStory.readOutData((param1.target as FileReference).data);
         if(_loc2_)
         {
            removeSelf();
         }
      }
      
      override public function act(param1:BtnBase) : void
      {
         var _loc2_:String = param1.name;
         if(_loc2_ == "back")
         {
            removeSelf();
         }
         else if(_loc2_ == "clean")
         {
            new UIReally(this.cleanYes,null,null,"Reset Data?","Wanna return to ZERO?",_sp,Attach.attach("UI_Really") as MovieClip,"UI_Really",_ui,this);
         }
         else if(_loc2_ == "save")
         {
            this.saveData();
         }
         else if(_loc2_ == "load")
         {
            new UIReally(this.loadYes,null,null,"Load Data?","Your data will be overwrited.",_sp,Attach.attach("UI_Really") as MovieClip,"UI_Really",_ui,this);
         }
      }
      
      public function saveData() : void
      {
         this.sl.saveAll(ConfigAndStory.makeOutData(),"AnBSave");
         MochiEvents.trackEvent("save_data");
      }
      
      public function cleanYes() : void
      {
         ConfigAndStory.cleanData();
         MochiEvents.trackEvent("clean_data");
      }
      
      public function loadYes() : void
      {
         this.sl.load();
         MochiEvents.trackEvent("load_data");
      }
   }
}
