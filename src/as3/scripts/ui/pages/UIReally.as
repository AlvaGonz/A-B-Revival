package ui.pages
{
   import f4.data.InitData;
   import flash.display.DisplayObjectContainer;
   import flash.display.MovieClip;
   import flash.text.TextField;
   import ui.UICtrl;
   import ui.base.BtnBase;
   import ui.base.UIBase;
   
   public class UIReally extends UIBase
   {
      
      public var okFunc:Function;
      
      public var cancelFunc:Function;
      
      public var inputWord:String;
      
      public function UIReally(param1:Function, param2:Function, param3:String, param4:String, param5:String, param6:DisplayObjectContainer, param7:MovieClip, param8:String, param9:UICtrl, param10:UIBase = null)
      {
         _mov = param7;
         if(!param3)
         {
            _mov.getChildByName("txtInput").visible = false;
            _mov.getChildByName("tnames").visible = false;
         }
         else
         {
            (_mov.getChildByName("txtInput") as TextField).text = param3;
         }
         (_mov.getChildByName("txtTitle") as TextField).text = param4;
         (_mov.getChildByName("txtWord") as TextField).text = param5;
         this.okFunc = param1;
         this.cancelFunc = param2;
         super(param6,param7,param8,param9,param10);
      }
      
      override public function act(param1:BtnBase) : void
      {
         var _loc2_:String = param1.name;
         if(_loc2_ == "yes")
         {
            this.inputWord = (_mov.getChildByName("txtInput") as TextField).text;
            removeSelf();
            if(this.okFunc != null)
            {
               this.okFunc();
            }
            InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[17]);
         }
         else if(_loc2_ == "no")
         {
            removeSelf();
            if(this.cancelFunc != null)
            {
               this.cancelFunc();
            }
            InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[18]);
         }
      }
   }
}

