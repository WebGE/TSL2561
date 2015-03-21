/**************************************************************** 
 * Driver for the TSL2561 digital luminosity (light) sensors.
 * @author   K.Townsend (Adafruit Industries) 
 * @license  BSD (see license.txt)
 ***************************************************************
 * TSL2561 library V1.0 (22/05/2014)  
 * Adaptation en C#  : Philippe Mariano 
 * Lycée Pierre Emile Martin 18000 Bourges  
****************************************************************/

using System;
using System.Threading;

using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;


namespace ToolBoxes
{
    /// <summary>
    /// TSL2561 Light to digital converter Class 
    /// </summary>
    public class TSL2561 : IDisposable
    {
        // I2C
        private const Int16 TRANSACTIONEXECUTETIMEOUT = 1000;
        private I2CDevice busI2C;
        private I2CDevice.Configuration configTSL2561;

        // TSL2561
        private IntegrationTime Itime = IntegrationTime._13MS;
        private Gain gain = Gain.x1;
        private UInt16 Channel0 = 0, Channel1 = 0;

//−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−
// Calculate lux Copyright  2004−2005 TAOS, Inc. www.taosinc.com
//−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−
        private const Byte LUX_LUXSCALE = 14; // scale by 2^14
        private const Byte LUX_RATIOSCALE = 9; // scale ratio by 2^9
//−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−
// Integration time scaling factors 
//−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−
        private const Byte LUX_CHSCALE = 10; // scale channel values by 2^10
        private const UInt16 LUX_CHSCALE_TINT0 = 0x7517; // 322/11 * 2^CH_SCALE
        private const UInt16 LUX_CHSCALE_TINT1 = 0x0fe7; // 322/81 * 2^CH_SCALE
//−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−
// T Package coefficients
//−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−
// For Ch1/Ch0=0.00 to 0.50
// Lux/Ch0=0.0304−0.062*((Ch1/Ch0)^1.4)
// piecewise approximation
// For Ch1/Ch0=0.00 to 0.125:
// Lux/Ch0=0.0304−0.0272*(Ch1/Ch0)
//
// For Ch1/Ch0=0.125 to 0.250:
// Lux/Ch0=0.0325−0.0440*(Ch1/Ch0)
//
// For Ch1/Ch0=0.250 to 0.375:
// Lux/Ch0=0.0351−0.0544*(Ch1/Ch0)
//
// For Ch1/Ch0=0.375 to 0.50:
// Lux/Ch0=0.0381−0.0624*(Ch1/Ch0)
//
// For Ch1/Ch0=0.50 to 0.61:
// Lux/Ch0=0.0224−0.031*(Ch1/Ch0)
//
// For Ch1/Ch0=0.61 to 0.80:
// Lux/Ch0=0.0128−0.0153*(Ch1/Ch0)
//
// For Ch1/Ch0=0.80 to 1.30:
// Lux/Ch0=0.00146−0.00112*(Ch1/Ch0)
//
// For Ch1/Ch0>1.3:
// Lux/Ch0=0
//−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−
        private const UInt16 K1T = 0x0040; // 0.125 * 2^RATIO_SCALE
        private const UInt16 B1T = 0x01f2; // 0.0304 * 2^LUX_SCALE
        private const UInt16 M1T = 0x01be; // 0.0272 * 2^LUX_SCALE

        private const UInt16 K2T = 0x0080; // 0.250 * 2^RATIO_SCALE
        private const UInt16 B2T = 0x0214; // 0.0325 * 2^LUX_SCALE
        private const UInt16 M2T = 0x02d1; // 0.0440 * 2^LUX_SCALE

        private const UInt16 K3T = 0x00c0; // 0.375 * 2^RATIO_SCALE
        private const UInt16 B3T = 0x023f; // 0.0351 * 2^LUX_SCALE
        private const UInt16 M3T = 0x037b; // 0.0544 * 2^LUX_SCALE

        private const UInt16 K4T = 0x0100; // 0.50 * 2^RATIO_SCALE
        private const UInt16 B4T = 0x0270; // 0.0381 * 2^LUX_SCALE
        private const UInt16 M4T = 0x03fe; // 0.0624 * 2^LUX_SCALE

        private const UInt16 K5T = 0x0138; // 0.61 * 2^RATIO_SCALE
        private const UInt16 B5T = 0x016f; // 0.0224 * 2^LUX_SCALE
        private const UInt16 M5T = 0x01fc; // 0.0310 * 2^LUX_SCALE

        private const UInt16 K6T = 0x019a; // 0.80 * 2^RATIO_SCALE
        private const UInt16 B6T = 0x00d2; // 0.0128 * 2^LUX_SCALE
        private const UInt16 M6T = 0x00fb; // 0.0153 * 2^LUX_SCALE

        private const UInt16 K7T = 0x029a; // 1.3 * 2^RATIO_SCALE
        private const UInt16 B7T = 0x0018; // 0.00146 * 2^LUX_SCALE
        private const UInt16 M7T = 0x0012; // 0.00112 * 2^LUX_SCALE

        private const UInt16 K8T = 0x029a; // 1.3 * 2^RATIO_SCALE
        private const UInt16 B8T = 0x0000; // 0.000 * 2^LUX_SCALE
        private const UInt16 M8T = 0x0000; // 0.000 * 2^LUX_SCALE
//−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−
// CS package coefficients
//−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−
// For 0 <= Ch1/Ch0 <= 0.52
// Lux/Ch0 = 0.0315−0.0593*((Ch1/Ch0)^1.4)
// piecewise approximation
// For 0 <= Ch1/Ch0 <= 0.13
// Lux/Ch0 = 0.0315−0.0262*(Ch1/Ch0)
// For 0.13 <= Ch1/Ch0 <= 0.26
// Lux/Ch0 = 0.0337−0.0430*(Ch1/Ch0)
// For 0.26 <= Ch1/Ch0 <= 0.39
// Lux/Ch0 = 0.0363−0.0529*(Ch1/Ch0)
// For 0.39 <= Ch1/Ch0 <= 0.52
// Lux/Ch0 = 0.0392−0.0605*(Ch1/Ch0)
// For 0.52 < Ch1/Ch0 <= 0.65
// Lux/Ch0 = 0.0229−0.0291*(Ch1/Ch0)
// For 0.65 < Ch1/Ch0 <= 0.80
// Lux/Ch0 = 0.00157−0.00180*(Ch1/Ch0)
// For 0.80 < Ch1/Ch0 <= 1.30
// Lux/Ch0 = 0.00338−0.00260*(Ch1/Ch0)
// For Ch1/Ch0 > 1.30
// Lux = 0
//−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−
        private const UInt16 K1C = 0x0043; // 0.130 * 2^RATIO_SCALE
        private const UInt16 B1C = 0x0204; // 0.0315 * 2^LUX_SCALE
        private const UInt16 M1C = 0x01ad; // 0.0262 * 2^LUX_SCALE

        private const UInt16 K2C = 0x0085; // 0.260 * 2^RATIO_SCALE
        private const UInt16 B2C = 0x0228; // 0.0337 * 2^LUX_SCALE
        private const UInt16 M2C = 0x02c1; // 0.0430 * 2^LUX_SCALE

        private const UInt16 K3C = 0x00c8; // 0.390 * 2^RATIO_SCALE
        private const UInt16 B3C = 0x0253; // 0.0363 * 2^LUX_SCALE
        private const UInt16 M3C = 0x0363; // 0.0529 * 2^LUX_SCALE

        private const UInt16 K4C = 0x010a; // 0.520 * 2^RATIO_SCALE
        private const UInt16 B4C = 0x0282; // 0.0392 * 2^LUX_SCALE
        private const UInt16 M4C = 0x03df; // 0.0605 * 2^LUX_SCALE

        private const UInt16 K5C = 0x014d; // 0.65 * 2^RATIO_SCALE
        private const UInt16 B5C = 0x0177; // 0.0229 * 2^LUX_SCALE
        private const UInt16 M5C = 0x01dd; // 0.0291 * 2^LUX_SCALE

        private const UInt16 K6C = 0x019a; // 0.80 * 2^RATIO_SCALE
        private const UInt16 B6C = 0x0101; // 0.0157 * 2^LUX_SCALE
        private const UInt16 M6C = 0x0127; // 0.0180 * 2^LUX_SCALE

        private const UInt16 K7C = 0x029a; // 1.3 * 2^RATIO_SCALE
        private const UInt16 B7C = 0x0037; // 0.00338 * 2^LUX_SCALE
        private const UInt16 M7C = 0x002b; // 0.00260 * 2^LUX_SCALE

        private const UInt16 K8C = 0x029a; // 1.3 * 2^RATIO_SCALE
        private const UInt16 B8C = 0x0000; // 0.000 * 2^LUX_SCALE
        private const UInt16 M8C = 0x0000; // 0.000 * 2^LUX_SCALE
//−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−
// Auto-gain thresholds
//−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−
        private const UInt16 AGC_THI_13MS = 4850;
        private const UInt16 AGC_TLO_13MS = 100;
        private const UInt16 AGC_THI_101MS = 36000;
        private const UInt16 AGC_TLO_101MS = 200;
        private const UInt16 AGC_THI_402MS = 63000;
        private const UInt16 AGC_TLO_402MS = 500;
//−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−
// Clipping thresholds
//−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−−
        private const UInt16 CLIPPING_13MS = 4900;
        private const UInt16 CLIPPING_101MS = 37000;
        private const UInt16 CLIPPING_402MS = 65000;

        /// <summary>
        /// TSL2561 Registers list
        /// </summary>
        private enum AddRegisters
        {                   // MSB = 1 + @ => register acces
            Control,        // @=00h: Control of basic function
            Timing,         // @=01h: Integration time/gain control
            Treshlowlow,    // @=02h: Low byte of low Interrupt threshold
            Treshlowhigh,   // @=03h: Low byte of high interrupt threshold
            Treshhighlow,   // @=04h: Low byte of low interrupt threshold
            Treshhighhigh,  // @=05h: Low high of high interrupt threshold
            Interrupt,      // @=06h: interrupt control
            Id = 0x0a,      // @=0ah: Part number / Rev Id
            Data0low = 0xc, // @=0ch: Low byte of ADC channel 0
            Data0high,      // @=0dh: high byte of ADC channel 0
            Data1low,       // @=0eh: Low byte of ADC channel 1
            Data1high       // @=0fh: high byte of ADC channel 1
        }
        /// <summary>
        /// TSL2561 Channels list
        /// </summary>
        private enum Channels
        {
            Channel0, 
            Channel1,
            TwoChannels
        }

        /// <summary>
        /// TSL2561 CommandByte list
        /// </summary>
        /// CommanByte = CommandBit | ClearBit | WordBit| BlockBit | ControlPowerOn | ControlPowerOff
        private enum CommandByte
        {          
            BlockBit = 0x10,
            WordBit = 0x20,
            ClearBit = 0x40,
            CommandBit = 0x80
        }

        /// <summary>
        /// TSL2561 ControlByte list
        /// </summary>
        private enum ControlByte
        {
            PowerOff,
            PowerOn = 0x03
        }

        /// <summary>
        /// IntegrationTime
        /// </summary>
        public enum IntegrationTime
        {
            _13MS,
           _101MS,
           _402MS
        }

        /// <summary>
        /// Gain x1 = 0xEF (0-> bit4) x16 = 0x10 (1->bit4)command
        /// </summary>
        public enum Gain
        {
            x16=0x10,
            x1 =0xEF          
        }
        
        /// <summary>
        /// Déconnexion virtuelle de l'objet Lcd du bus I2C
        /// </summary>
        public void Dispose()
        {
            this.busI2C.Dispose(); // Déconnexion virtuelle de l'objet Lcd du bus I2C
        }

        /// <summary>
        /// Constructor with Slave Address = 0x29 and Bus Frequency = 100kHz
        /// </summary>
        public TSL2561()
        { 
            configTSL2561 = new I2CDevice.Configuration(0x29, 100); 
        }  
      
        /// <summary>
        /// Constructor with Bus Frequency = 100kHz
        /// </summary>
        /// <param name="I2C_Add_7bits">ADDR SEL TERMINAL LEVEL: (GND)0x29, (VDD)0x49, (FLOAT)0x69</param>
        public TSL2561(byte I2C_Add_7bits=0x29)
        {
            configTSL2561 = new I2CDevice.Configuration(I2C_Add_7bits, 100);
        }    

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="I2C_Add_7bits">ADDR SEL: (GND)0x29, (VDD)0x49, (FLOAT)0x69</param>
        /// <param name="Frequency">400 khz max</param>
        public TSL2561(byte I2C_Add_7bits=0x29, UInt16 Frequency=100)
        {
            configTSL2561 = new I2CDevice.Configuration(I2C_Add_7bits, Frequency);
        }        

        /// <summary>
        /// Control Register Get Access
        /// </summary>
        public Byte control
         {
             
             get {
                    Enable();
                    Byte V_Control = GetRegister(AddRegisters.Control);
                    Disable();
                    return V_Control;                  
             }
         }

        /// <summary>
        /// Timing Register Get Access
        /// </summary>
        public Byte timing
         {
            get {
                Enable();
                Byte V_Timing = GetRegister(AddRegisters.Timing);
                Disable();
                return V_Timing; 
            }
         }

        /// <summary>
        /// Threshlowlow Register Get Access
        /// </summary>
        public Byte threshLowLow
         {
             get
             {
                 Enable();
                 Byte V_ThreshLowLow = GetRegister(AddRegisters.Treshlowlow);
                 Disable();
                 return V_ThreshLowLow;
             }
             set
             {
                 SetRegister(AddRegisters.Treshlowlow, value);
             }
         }

        /// <summary>
        /// Threshlowhigh Register Get Access
        /// </summary>
        public Byte threshLowhigh
         {
             get
             {
                 Enable();
                 Byte V_ThreshLowhigh = GetRegister(AddRegisters.Treshlowhigh);
                 Disable();
                 return V_ThreshLowhigh;
             }
             set
             {
                 SetRegister(AddRegisters.Treshlowhigh, value);
             }
         }

        /// <summary>
        /// Threshhighlow Register Get Access
        /// </summary>
        public Byte threshhighLow
         {
             get
             {
                 Enable();
                 Byte V_ThreshhighLow = GetRegister(AddRegisters.Treshhighlow);
                 Disable();
                 return V_ThreshhighLow;
             }
             set
             {
                 SetRegister(AddRegisters.Treshhighlow, value);
             }
         }

        /// <summary>
        /// Threshhighhigh Register Get Access
        /// </summary>
        public Byte threshhighhigh
         {
             get
             {
                 Enable();
                 Byte V_Threshhighhigh = GetRegister(AddRegisters.Treshhighhigh);
                 Disable();
                 return V_Threshhighhigh;
             }
             set
             {
                 SetRegister(AddRegisters.Treshhighhigh, value);
             }
         }  

        /// <summary>
        /// Interrupt Register Get Access
        /// </summary>
        public Byte interrupt
        {
            get
            {
                Enable();
                Byte V_Interrupt = GetRegister(AddRegisters.Interrupt);
                Disable();
                return V_Interrupt;
            }
        }

        /// <summary>
        /// Id Register Get Access
        /// </summary>
        public Byte id
        {
            get
            {
                Enable();
                byte V_Id = GetRegister(AddRegisters.Id);
                Disable();
                return V_Id;
            }
        }
        /*
        /// <summary>
        /// Data0Low Register Get Access
        /// </summary>
        public Byte data0Low
        {
            get
            {
                byte V_Data0Low = GetRegister((byte)CommandByte.CommandBit | (byte)AddRegisters.Con);
                return V_Data0Low;
            }
        }
        /// <summary>
        /// Data0High Register Get Access
        /// </summary>
        public Byte data0High
        {
            get
            {
                byte V_Data0High = GetRegister(Registers.Data0high);
                return V_Data0High;
            }
        }        
        /// <summary>
        /// Data1Low Register Get Access
        /// </summary>
        public Byte data1Low
        {
            get
            {
                byte V_Data1Low = GetRegister(Registers.Data1low);
                return V_Data1Low;
            }
        }
        /// <summary>
        /// Data1High Register Get Access
        /// </summary>
        public Byte data1High
        {
            get
            {
                byte V_Data1High = GetRegister(Registers.Data1high);
                return V_Data1High;
            }
        }  */
        /// <summary>
        /// Channel0 Register Get Access
        /// </summary>
        public UInt16 channel0
        {
            get 
                {
                    
                    UInt16 channel0 = getData(Channels.Channel0);
                    return channel0; 
                }
        }        
        /// <summary>
        /// Channel1 Register Get Access
        /// </summary>
        public UInt16 channel1
        {
            get
            {
                UInt16 channel1 = getData(Channels.Channel1);
                return channel1;
            }
        }
        
        
        /// <summary>
        /// TSL2561 Initialisation : 
        /// Power Up, IRQ disable,Power Down
        /// Gain = 1, Integrate time = 13ms
        /// </summary>
        public void Init()
        {
            byte value = 0;
            Enable();
            value = (byte)((value | (byte)IntegrationTime._402MS) & (byte)Gain.x1);
            SetRegister(AddRegisters.Timing, value);
            SetRegister(AddRegisters.Interrupt, 0x00);
            Disable();
        }

        public void Init(Gain gain = Gain.x1, IntegrationTime IT = IntegrationTime._13MS)
        {
            byte value = 0;
            Enable();
            value = (byte)((value | (byte)IT) & (byte)gain);
            SetRegister(AddRegisters.Timing, value);
            SetRegister(AddRegisters.Interrupt, 0x00);
            Disable();
        }

        /*
        /// <summary>
        /// TSL2561 Initialisation :
        /// Power Up, IRQ disable,
        /// Gain: 0(x1) 1(x16) INTEG: 0(13,7ms) 1(101ms) 2(402ms) 3(Manual)
        /// </summary>
        /// <param name="valueTimingRegister">bit4=Gain bit1..0=INTEG</param>
        public void Init(byte valueTimingRegister=0x11)
        {
            SetRegister(AddRegisters.Control, 0x03);
            SetRegister(AddRegisters.Timing, valueTimingRegister);
            SetRegister(AddRegisters.Interrupt, 0x00);
        }
        */
        /// <summary>
        /// Write a byte in a register
        /// </summary>
        /// <param name="add">register adress</param>
        /// <param name="value">value to write</param>
        /// <returns></returns>
        private int SetRegister(AddRegisters add, byte value)
        {
            byte command = (byte)((byte)CommandByte.CommandBit | (byte)add); 
            // Création d'un buffer et d'une transaction pour l'accès au circuit en écriture
            byte[] outbuffer = new byte[] { command, value };
            I2CDevice.I2CTransaction writeTransaction = I2CDevice.CreateWriteTransaction(outbuffer);
            // Tableaux des transactions 
            I2CDevice.I2CTransaction[] T_WriteByte = new I2CDevice.I2CTransaction[] { writeTransaction };
            busI2C = new I2CDevice(configTSL2561); // Connexion virtuelle de l'objet TSL2561 au bus I2C 
            busI2C.Execute(T_WriteByte, TRANSACTIONEXECUTETIMEOUT); // Exécution de la transaction
            busI2C.Dispose(); // Déconnexion virtuelle de l'objet TSL2561 du bus I2C
            return 1;
        }

        /// <summary>
        /// Read a byte from a register
        /// </summary>
        /// <param name="add">register adress</param>
        /// <returns>register value</returns>
        private byte GetRegister(AddRegisters add)
        {
            byte command = (byte)((byte)CommandByte.CommandBit | (byte)add); 
            // Buffer d'écriture
            byte[] outBuffer = new byte[] { command };
            I2CDevice.I2CWriteTransaction writeTransaction = I2CDevice.CreateWriteTransaction(outBuffer);

            // Buffer de lecture
            byte[] inBuffer = new byte[1];
            I2CDevice.I2CReadTransaction readTransaction = I2CDevice.CreateReadTransaction(inBuffer);

            // Tableau des transactions
            I2CDevice.I2CTransaction[] transactions = new I2CDevice.I2CTransaction[] { writeTransaction, readTransaction };
            // Exécution des transactions
            busI2C = new I2CDevice(configTSL2561); // Connexion virtuelle du TSL2561 au bus I2C

            if (busI2C.Execute(transactions, TRANSACTIONEXECUTETIMEOUT) != 0)
            {
                // Success
                //Debug.Print("Received the first data from at device " + busI2C.Config.Address + ": " + ((int)inBuffer[0]).ToString());            
            }
            else
            {
                // Failed
                //Debug.Print("Failed to execute transaction at device: " + busI2C.Config.Address + ".");
            }
            busI2C.Dispose(); // Déconnexion virtuelle de l'objet Lcd du bus I2C
            return inBuffer[0];           
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="add"></param>
        /// <returns></returns>
        private UInt16 GetWord(AddRegisters add)
        {
            UInt16 word = 0;
            byte command = (byte)((byte)CommandByte.CommandBit | (byte)CommandByte.WordBit | (byte)add);
            // Buffer d'écriture
            byte[] outBuffer = new byte[] { command };
            I2CDevice.I2CWriteTransaction writeTransaction = I2CDevice.CreateWriteTransaction(outBuffer);

            // Buffer de lecture
            byte[] inBuffer = new byte[2];
            I2CDevice.I2CReadTransaction readTransaction = I2CDevice.CreateReadTransaction(inBuffer);

            // Tableau des transactions
            I2CDevice.I2CTransaction[] transactions = new I2CDevice.I2CTransaction[] { writeTransaction, readTransaction };
            // Exécution des transactions
            busI2C = new I2CDevice(configTSL2561); // Connexion virtuelle du TSL2561 au bus I2C

            if (busI2C.Execute(transactions, TRANSACTIONEXECUTETIMEOUT) != 0)
            {
                // Success
               word =  (UInt16)(((UInt16)inBuffer[1] * 256) | (inBuffer[0]));           
            }
            else
            {
                // Failed
            }
            busI2C.Dispose(); // Déconnexion virtuelle de l'objet Lcd du bus I2C
            return word;
        }
        /// <summary>
        /// TSL2561 power up
        /// </summary>
        private void Enable()
        {
            SetRegister(AddRegisters.Control, (byte)ControlByte.PowerOn);
        }
        /// <summary>
        /// TSL2561 power down
        /// </summary>
        private void Disable()
        {
            SetRegister(AddRegisters.Control, (byte)ControlByte.PowerOff);
        }

        private UInt16 getData(Channels channel)
        {
            UInt16 value = 0;
            // Enable the device
            Enable();

            // Wait x ms for ADC to complete
            switch (Itime)
            {
                case IntegrationTime._13MS:
                    Thread.Sleep(14);
                    break;
                case IntegrationTime._101MS:
                    Thread.Sleep(102);
                    break;
                default:
                    Thread.Sleep(403);
                    break;
            }

            switch (channel)
            {
                case Channels.Channel0:// Read a two byte value from channel 0 (visible + infrared)                    
                    value = GetWord(AddRegisters.Data0low);
                    break;
                case Channels.Channel1:// Read a two byte value from channel 1 (infrared)                   
                    value = GetWord(AddRegisters.Data1low);
                    break;
                default:
                    Channel0 = GetWord(AddRegisters.Data0low);
                    Channel1 = GetWord(AddRegisters.Data1low);
                    break;
            }
            Disable();
            return value;           
        }
        
        

// lux equation approximation without floating point calculations
//////////////////////////////////////////////////////////////////////////////
// Methode : unsigned int CalculateLux()
// Description: Calculate the approximate illuminance (lux) given the raw
// channel values of the TSL2560. The equation if implemented
// as a piece−wise linear approximation.
//
// Arguments readed
// byte Gain − gain, where 0:1X, 1:16X
// byte integ − integration time, where 0:13.7mS, 1:100mS, 2:402mS, 3:Manual
// unsigned int channel0 − raw channel value from channel 0 of TSL2560
// unsigned int channel1 − raw channel value from channel 1 of TSL2560
// Fixed
// bool Type − package type (T or CS)
//
// Return: unsigned int − the approximate illuminance (lux)      
//////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Calculate lux for Adafruit or Grove sensor 
        /// </summary>
        /// <returns>lux</returns>
        public float calculateLux()
        {
          
           UInt64 chScale;
           UInt64 CH0, CH1;
           bool Type = false;   // False : Package T True : Package CS 

            // Read Channels
           getData(Channels.TwoChannels);

            // Make sure the sensor isn't satured
           UInt16 clipThreshold;
           switch (Itime)
           {
               case IntegrationTime._13MS:
                   clipThreshold = CLIPPING_13MS;
                   break;
               case IntegrationTime._101MS:
                   clipThreshold = CLIPPING_101MS;
                   break;
              default:
                   clipThreshold = CLIPPING_402MS;
                   break;
           }
            // return 0 Lux if the sensor is satured 
           if ((Channel0 > clipThreshold)||(Channel1>clipThreshold))
           {
               return 0;
           }

            // Get the correct scale depending on the integration time
           switch (Itime)
           {
               case IntegrationTime._13MS:
                   chScale = LUX_CHSCALE_TINT0;
                   break;
               case IntegrationTime._101MS:
                   chScale = LUX_CHSCALE_TINT1;
                   break;             
               default: // No scaling ... integration time = 402ms
                   chScale = (1 << LUX_CHSCALE);
                   break;
           }
            // Scale for Gain (x1 or x16)
           if ((byte)gain != 16)
           {
               chScale = chScale << 4;  
           }
            // Scale the channel value
           CH0 = (Channel0 * chScale) >> LUX_CHSCALE;
           CH1 = (Channel1 * chScale) >> LUX_CHSCALE;

            // Find the ratio of the channel values (Channel1/Channel0)
           UInt64 ratio1 = 0;
           if (CH0 != 0) 
           {
               ratio1 = (CH1 << (LUX_RATIOSCALE + 1)) / CH0;
           }
            // round the ratio value
           UInt64 ratio = (ratio1 + 1) >> 1;

           UInt32 b=0, m=0;

            switch (Type)
            {
                case false: // T package
                    if ((ratio >= 0) && (ratio <= K1T))
                    {
                        b = B1T; m = M1T;
                    }
                    else if (ratio <= K2T)
                    {
                        b = B2T; m = M2T;
                    }
                    else if (ratio <= K3T)
                    {
                        b = B3T; m = M3T;
                    }
                    else if (ratio <= K4T)
                    {
                        b = B4T; m = M4T;
                    }
                    else if (ratio <= K5T)
                    {
                        b = B5T; m = M5T;
                    }
                    else if (ratio <= K6T)
                    {
                        b = B6T; m = M6T;
                    }
                    else if (ratio <= K7T)
                    {
                        b = B7T; m = M7T;
                    }
                    else if (ratio > K8T)
                    {
                        b = B8T; m = M8T;
                    }
                break;

                case true: // CS package
                    if ((ratio >= 0) && (ratio <= K1C))
                    {
                        b = B1C; m = M1C;
                    }
                    else if (ratio <= K2C)
                    {
                        b = B2C; m = M2C;
                    }
                    else if (ratio <= K3C)
                    {
                        b = B3C; m = M3C;
                    }
                    else if (ratio <= K4C)
                    {
                        b = B4C; m = M4C;
                    }
                    else if (ratio <= K5C)
                    {
                        b = B5C; m = M5C;
                    }
                    else if (ratio <= K6C)
                    {
                        b = B6C; m = M6C;
                    }
                    else if (ratio <= K7C)
                    {
                        b = B7C; m = M7C;
                    }
                break;
            }

            UInt64 temp = ((CH0 * b) - (CH1 * m));

            // do not allow negative lux value
            if (temp < 0)
            {
                temp = 0;
            }
            // round lsb (2^(LUX_SCALE−1))
            temp += (1 << (LUX_LUXSCALE - 1));

            // strip off fractional portion
            float lux = (UInt32)(temp >> LUX_LUXSCALE);

            return lux;
        }
    }
 }
