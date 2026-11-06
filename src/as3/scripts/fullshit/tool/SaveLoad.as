package fullshit.tool
{
   import flash.events.Event;
   import flash.net.FileFilter;
   import flash.net.FileReference;
   import flash.utils.ByteArray;
   
   public class SaveLoad
   {
      
      private var _fileL:FileReference;
      
      private var _fileS:FileReference;
      
      public var fileExt:String;
      
      public function SaveLoad(param1:String, param2:Function)
      {
         super();
         this.fileExt = param1;
         this._fileS = new FileReference();
         this._fileL = new FileReference();
         this._fileL.addEventListener(Event.SELECT,this.selMap);
         this._fileL.addEventListener(Event.COMPLETE,param2);
      }
      
      private function selMap(param1:Event) : void
      {
         this._fileL.load();
      }
      
      public function load() : void
      {
         this._fileL.browse([new FileFilter(this.fileExt,"*." + this.fileExt)]);
      }
      
      public function saveAll(param1:ByteArray, param2:String) : void
      {
         this._fileS.save(param1,param2 + "." + this.fileExt);
      }
   }
}

