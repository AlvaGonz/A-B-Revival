package f4data.sdata
{
   import f4.data.InitData;
   import f4data.movie.MovieIndex;
   import f4data.talk.TalkIndex;
   import flash.display.MovieClip;
   import fullshit.ctrl2.SoundUnit2;
   import fullshit.tool.Attach;
   import ui.base.UIBase;
   import ui.pages.UIMovie;
   import ui.pages.UITalk;
   
   public class Director
   {
      
      public var talk:TalkIndex;
      
      public var movie:MovieIndex;
      
      private var _tend:Function;
      
      private var _tpage:UIBase;
      
      public function Director()
      {
         super();
         this.talk = ConfigAndStory.ALLPART.talk;
         this.movie = ConfigAndStory.ALLPART.movie;
      }
      
      public function addTalk(param1:Function, param2:UIBase, param3:Array, param4:Boolean, param5:String) : void
      {
         if(param4)
         {
            new UITalk(param1,param3,param2.sp,Attach.attach(param5) as MovieClip,"UI_Talk",param2.uictrl,param2);
         }
         else
         {
            new UITalk(param1,param3,param2.sp,Attach.attach(param5) as MovieClip,"UI_Talk",param2.uictrl);
         }
      }
      
      public function addMovie(param1:Function, param2:UIBase, param3:Array, param4:Boolean) : void
      {
         if(param4)
         {
            new UIMovie(param1,param3,param2.sp,Attach.attach("UI_Movie") as MovieClip,"UI_Movie",param2.uictrl,param2);
         }
         else
         {
            new UIMovie(param1,param3,param2.sp,Attach.attach("UI_Movie") as MovieClip,"UI_Movie",param2.uictrl);
         }
      }
      
      public function beforeLevelMov(param1:Function, param2:UIBase) : void
      {
         var _loc3_:Array = null;
         InitData.SOUNDC2.allEnd(true,"M_Menu");
         InitData.SOUNDC2.addSound(new SoundUnit2(InitData.SOUNDDATA.musicArray[1],"M_Menu"),int.MAX_VALUE,0.01,InitData.SOUNDC2.nowVol * 0.25);
         this._tend = param1;
         this._tpage = param2;
         if(this.movie.movIndex[ConfigAndStory.STORY] != null)
         {
            _loc3_ = this.movie.movIndex[ConfigAndStory.STORY][0];
         }
         if(ConfigAndStory.STORY == 0)
         {
            if(_loc3_)
            {
               this.addMovie(this.beforeLevelTalk,param2,_loc3_,false);
            }
            else
            {
               this.beforeLevelTalk();
            }
         }
         else
         {
            if(_loc3_)
            {
               this.addMovie(this._tend,param2,_loc3_,false);
            }
            else
            {
               this._tend();
            }
            this._tend = null;
            this._tpage = null;
         }
      }
      
      public function beforeLevelTalk() : void
      {
         var _loc1_:Array = this.talk.talkBefore[ConfigAndStory.STORY];
         if(_loc1_)
         {
            this.addTalk(this._tend,this._tpage,_loc1_,true,"UI_TalkX");
         }
         else
         {
            this._tend();
         }
         this._tend = null;
         this._tpage = null;
      }
      
      public function afterLevelMov(param1:Function, param2:UIBase) : void
      {
         var _loc3_:Array = null;
         InitData.SOUNDC2.allEnd(true,"M_Menu");
         InitData.SOUNDC2.addSound(new SoundUnit2(InitData.SOUNDDATA.musicArray[1],"M_Menu"),int.MAX_VALUE,0.01,InitData.SOUNDC2.nowVol * 0.25);
         this._tend = param1;
         this._tpage = param2;
         if(this.movie.movIndex[ConfigAndStory.STORY] != null)
         {
            _loc3_ = this.movie.movIndex[ConfigAndStory.STORY][1];
         }
         if(_loc3_)
         {
            this.addMovie(this.beforeLevelTalk,param2,_loc3_,false);
         }
         else
         {
            this.beforeLevelTalk();
         }
      }
      
      public function afterLevelTalk(param1:Function, param2:UIBase) : void
      {
         this._tend = param1;
         this._tpage = param2;
         var _loc3_:Array = this.talk.talkAfter[ConfigAndStory.STORY];
         if(_loc3_)
         {
            this.addTalk(this._tend,this._tpage,_loc3_,true,"UI_TalkX");
         }
         else
         {
            this._tend();
         }
         this._tend = null;
         this._tpage = null;
      }
      
      public function beforePlace(param1:Function, param2:UIBase, param3:int, param4:int, param5:int) : void
      {
         var _loc6_:Array = this.talk.placeIndex[param3][param4][param5];
         if(_loc6_)
         {
            this.addTalk(param1,param2,_loc6_,true,"UI_Talk");
         }
         else
         {
            param1();
         }
      }
   }
}

