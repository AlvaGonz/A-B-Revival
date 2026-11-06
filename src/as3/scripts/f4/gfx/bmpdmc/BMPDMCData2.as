package f4.gfx.bmpdmc
{
   public class BMPDMCData2
   {
      
      public var bmpd1:BMPDMCData1;
      
      public var bmpdName:String;
      
      public function BMPDMCData2(param1:String, param2:BMPDMCData1)
      {
         super();
         this.bmpd1 = param2;
         this.bmpdName = param1;
      }
      
      public function makeBMPD2D(param1:Boolean) : BMPD2D
      {
         return new BMPD2D(param1,this.bmpd1.makeBMPDMC());
      }
   }
}

