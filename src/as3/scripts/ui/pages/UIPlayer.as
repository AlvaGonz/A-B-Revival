package ui.pages
{
   import f4.data.InitData;
   import flash.display.DisplayObjectContainer;
   import flash.display.MovieClip;
   import flash.text.TextField;
   import fullshit.ctrl2.SoundUnit2;
   import mochi.as3.MochiEvents;
   import ui.UICtrl;
   import ui.base.BtnBase;
   import ui.base.UIBase;
   
   public class UIPlayer extends UIBase
   {
      
      public var endFunc:Function;
      
      public function UIPlayer(param1:DisplayObjectContainer, param2:MovieClip, param3:String, param4:UICtrl, param5:UIBase = null, param6:Function = null)
      {
         var _loc7_:int = Math.random() * InitData.SOUNDDATA.songNames.length;
         (param2.getChildByName("txtWords") as TextField).text = "Music Track #" + (_loc7_ + 1) + "\n\n\"" + InitData.SOUNDDATA.songNames[_loc7_] + "\"";
         (param2.getChildByName("txtTitle") as TextField).text = "RandomPlayer";
         this.endFunc = param6;
         super(param1,param2,param3,param4,param5);
         InitData.SOUNDC2.allEnd(true);
         InitData.SOUNDC2.addSound(new SoundUnit2(InitData.SOUNDDATA.musicArray[_loc7_],"M_Player" + _loc7_),int.MAX_VALUE,0.01,1);
         MochiEvents.trackEvent("open_player");
      }
      
      override public function act(param1:BtnBase) : void
      {
         var _loc2_:String = param1.name;
         if(_loc2_ == "ok")
         {
            if(this.endFunc != null)
            {
               this.endFunc();
            }
            InitData.SOUNDC2.allEnd(true);
            InitData.SOUNDC2.addSound(new SoundUnit2(InitData.SOUNDDATA.musicArray[0],"M_Main"),int.MAX_VALUE,0.01,InitData.SOUNDC2.nowVol);
            removeSelf();
         }
      }
   }
}

