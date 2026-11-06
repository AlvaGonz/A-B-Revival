package f4.gfx.bmpdmc
{
   import flash.display.BitmapData;
   import flash.geom.Rectangle;
   
   public class BMPDMCData1
   {
      
      public var bmpdName:String;
      
      public var bmpd:BitmapData;
      
      public var rect:Rectangle;
      
      public var frames:int;
      
      public function BMPDMCData1(param1:int, param2:Rectangle, param3:BitmapData, param4:String)
      {
         super();
         this.frames = param1;
         this.rect = param2.clone();
         this.bmpd = param3;
         this.bmpdName = param4;
      }
      
      public function makeBMPDMC() : BMPDMC
      {
         return new BMPDMC(this.frames,this.rect.clone(),this.bmpd,this.bmpdName);
      }
   }
}

