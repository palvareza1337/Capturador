//#define NO_WISER3X
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;

namespace Capturador
{
    public class Phantom
    {
        static public string version = "134";

        private const string multicast_ip_address = "230.230.230.230";
        private const int ttl = 5;
        private const int tcp_port = 11050;
        private const int udp_port = 11050;


        private const int temperature_count = 4;
        private const int alarm_count = 12;

        private static bool ack_packages = false;

        public delegate void ProcessPhantomMessages(Messages message, MessageData data);

        public static TcpClient WiserTCPClient = null;
        public static TcpClient WiserTCPClient2 = null;
        public static TcpListener tcp_server = null;
        public static TcpListener wiser3x_server = null;
        public static UdpClient udp_server = null;
        public static bool should_stop_threads = false;
        public static bool wiserStartCommandSent = false;
        public bool isWiserStarted
        {
            get
            {
                if (wiserStartCommandSent ==  false){
                    return false;
                }
                else
                {
                    return !should_stop_threads;
                }
                
            }
            
        }

        public static ProcessPhantomMessages m_messageProcessor;

        public const string EIMO1 = "EIMO1";
        public const string EIMONanoAccelLR = "EINALR";
        public const string EIMONanoAccelHR = "EINAHR";
        public const string EIMONanoTempNC = "EINTNC";
        public const string EIMONanoTempTP = "EINTTP";
        public const string EIMONanoTempTPV2 = "EINTTPV2";
        public const string EIMONanoRPM = "EINRPM";
        public const string EIMONanoVoltage = "EINVoltage";
        public const string EIMONanoCurrent = "EINCurrent";
        public const string EIMONanoCurrentV2 = "EINCurrentV2";
        public const string EIMONanoBiaxial = "EINABA";
        public const string EIMONanoAccelGP = "EINAGP";
        public const string EIMONano420 = "EI420";
        public const string EIMONanoDigitalIn = "EIDIN";
        public const string EIMONanoVoltimeter = "EIVOLT";
        public const string EIMONanoUniversal = "EINAU";
        public const string EIMONanoThermalCamera = "EITHRCAM";

        private const int udp_message_eimoinfo = 3;
        private const int udp_message_eimoerror = 4;
        private const int udp_message_eimotemperature = 5;
        private const int udp_message_eimosettings = 6;

        private const int eimo_data_header_length = 32;



        public enum Messages
        {
            RECEIVED_UNKNOWMESSAGE,
            RECEVIED_SMALLMESSAGE_BYTES,
            RECEIVED_BIGMESSAGE_BYTES,
            RECEIVED_EIMOINFO,
            RECEIVED_PHANTOM_ACCEL_DATA,
            RECEIVED_PHANTOM_TEMP_DATA,
            RECEIVED_PHANTOM_420_DATA,
            RECEIVED_ID_INFO,
            RECEIVED_PHANTOM_ACCEL_SETTINGS,
            RECEIVED_PHANTOM_ACCEL_STATE,
            RECEIVED_PHANTOM_CURRENT_DATA,
            RECEIVED_PHANTOM_CURRENT_DATAV2,
            RECEIVED_PHANTOM_ACCUMULATED_CURRENT_DATA,
            RECEIVED_PHANTOM_RPM_DATA,
            RECEIVED_PHANTOM_GP_SETTINGS,
            RECEIVED_PHANTOM_GP_STATE,
            RECEIVED_PHANTOM_THERMAL_IMAGE,
            RECEIVED_PHANTOM_THERMAL_DATA,
            RECEIVED_PHANTOM_ACCEL_SETTINGS2,
            RECEIVED_PHANTOM_DIGITAL_INPUTS_DATA,
            RECEIVED_PHANTOM_RSSI_DATA,
            RECEIVED_PHANTOM_VOLTAGE_DATA,
        }

        public enum Reason
        {
            REQUESTED = 1,
            SCHEDULED = 2,
            ALARM = 3,
            SOFTRESET = 5,
			TRIGGERED = 6,
        }

        public enum TriggerType
        {
            RPM = 1,
            CURRENT = 2,
        }

        public enum RecordMode
        {
            AUTO = 1,
            MANUAL = 2
        }

        public enum TemperatureType
        {
            THERMOCOUPLE = 1,
            INFRARED = 2
        }

        private enum ENANODeviceTypes
        {
            ENANO_ACCEL_3X_HR = 3,     // KX222
            ENANO_ACCEL_3X_HR_V2 = 6, // KX134
            ENANO_ACCEL_3X_LR = 5,    // KX122
            ENANO_ACCEL_UNIVERSAL = 7,
            ENANO_THERMAL_CAMERA = 10,
            ENANO_TEMPERATURE_CONTACT = 20,
            ENANO_TEMPERATURE_CONTACTV2 = 21,
            ENANO_TEMPERATURE_INFRARED = 25,
            ENANO_420 = 26,
            ENANO_VOLTIMETER = 27,
            ENANO_CURRENT = 30,
            ENANO_DIGITA_INPUTS = 31,
            ENANO_CURRENT_V2 = 32,
            ENANO_RPM = 40,
            ENANO_BIAXIAL = 50,
            ENANO_GP = 60,

        }

        public enum BatteryType
        {
            ONESIXTH_D = 1,
            TWOxAAA = 2,
            CR2032 = 3,
        }

        public enum SensorType
        {
            KX222 = 1,
            KX122 = 2,
            KX134 = 3,
        }

        public class MessageData
        {
            public string device;
            public string phantom_code;
            public BatteryType battery_type = BatteryType.ONESIXTH_D;
            public DateTime time_data;
        }

        public class MessageDataEIMONanoAccel : MessageData
        {
            public Reason recording_reason;
            //public string phantom_code;
            public float temperature;
            public short[] channel1;
            public short[] channel2;
            public short[] channel3;
            public double calibration;
            public int sample_rate;
            public int sequence;
            public TriggerType trigger_type;
            public double trigger;
            public bool hasrealtime = false;
            public DateTime realnodetime;
            public SensorType sensor_type;
        }

        public class MessageDataEIMONanoTemp : MessageData
        {
            public Reason recording_reason;
            //public string phantom_code;
            public TemperatureType type;

            public bool temp1fault = false;
            public bool temp2fault = false;
            public bool temp3fault = false;


            public float temperature;
            public float temperature2;
            public float temperature3;
            public float battery;
            public float ambient_temperature;
            public float module_temperature;
            public uint version;
            public uint seq;
        }

        public class MessageData420 : MessageData
        {
            public Reason recording_reason;
            //public string phantom_code;
            public float battery;
            public float module_temperature;
            public float channel1_voltage;
            public float channel2_voltage;
            public float channel3_voltage;
            public float channel4_voltage;
            public uint version;
            public uint seq;
        }
        public class MessageVoltimeter : MessageData
        {
            public Reason recording_reason;
            public float battery;
            public float module_temperature;
            public float channel1_voltage;
            public float channel2_voltage;
            public float channel3_voltage;
            public float channel4_voltage;
            public uint version;
            public uint seq;
        }


        public class ThermalCameraImage : MessageData
        {
            public Reason recording_reason;
            //public string phantom_code;
            public uint version;
            public uint sequence;

            public float[,,] temp_data;

            public int image_rows;
            public int image_columns; 
            public int image_frames;
        }
        public class ThermalCameraData : MessageData
        {
            public float battery;
            public float module_temperature;
            public float average_frame_temperature;
            public float average_roi_temperature;
            public uint version;
        }


        public class MessageDataIDInfo : MessageData
        {
            public string ip_address;
            //public string phantom_code;

            public string wf_version;
            public string bt_version;
        }


        public class MessageIdWiser3x : MessageData
        {
            public uint wiser_id;
            public int version;
        }


        public class MessageEIMONanoAccelSettings : MessageData
        {
            //public string phantom_code;
            public int send_interval;
            public int sample_rate;
            public int samples_to_get;
            public int range;
            public float alarm1;
            public float alarm2;
            public float alarm3;
            public int alarmcheck_interval;
            public string source_ip;
        }

        public class MessageEIMONanoAccelSettings2 : MessageData
        {
            //public string phantom_code;
            public int max_rpm;
            public int min_rpm;
            public string rpm_phantom_id;
        }


        public class MessageEIMONanoAccelState : MessageData
        {
            //public string phantom_code;
            public float battery;
            public float temperature;
            public float rms1;
            public float rms2;
            public float rms3;
            public uint version;
        }

        public class MessageEIMONanoCurrentData : MessageData
        {
            //public string phantom_code;
            public float battery;
            public float module_temperature;
            public float current1;
            public float current2;
            public float current3;
            public uint version;
            public uint seq;
        }

        public class MessageEIMONanoCurrentDataV2 : MessageData
        {
            //public string phantom_code;
            public float battery;
            public float module_temperature;

            public float min_current1;
            public float min_current2;
            public float min_current3;
            public float min_current4;

            public float max_current1;
            public float max_current2;
            public float max_current3;
            public float max_current4;

            public float avg_current1;
            public float avg_current2;
            public float avg_current3;
            public float avg_current4;

            public uint version;
            public uint seq;
        }

        public class MessageEIMONanoAccumulatedCurrentData : MessageData
        {
            //public string phantom_code;

            public float accumulated_current1;
            public float accumulated_current2;
            public float accumulated_current3;
            public float accumulated_current4;

            public DateTime startPeriod;
            public DateTime endPeriod;
        }


        public class MessageEIMONanoDigitalInData : MessageData
        {
            //public string phantom_code;
            public float battery;
            public float module_temperature;
            public bool input1;
            public bool input2;
            public bool input3;
            public bool input4;
            public uint version;
            public uint seq;
        }


        public class MessageEIMONanoRPMData : MessageData
        {
            //public string phantom_code;
            public float battery;
            public float module_temperature;
            public float trigger;
            public uint version;
            public uint seq;
        }

        public class MessageEIMONanoGPSettings : MessageData
        {
            //public string phantom_code;
            public int send_interval;
            public int sample_rate;
            public int samples_to_get;
            public int range;
            public float alarm1;
            public float alarm2;
            public int alarmcheck_interval;
        }
        public class MessageEIMONanoGPAccelState : MessageData
        {
            //public string phantom_code;
            public float battery;
            public float temperature;
            public float rms1;
            public float rms2;
            public float vpp_min1;
            public float vpp_max1;

            public float vpp_min2;
            public float vpp_max2;

            public float vpp_min01;
            public float vpp_max01;

            public float vpp_min02;
            public float vpp_max02;
        }

        public class MessageEIMORSSIData : MessageData
        {
            //public string phantom_code;
            public int max_rssi;
            public int min_rssi;
            public int max_channel;
            public int min_channel;
        }


        public class MessageEIMORaw : MessageData
        {
            public Messages inner_message;
            public MessageData inner_data;

            public byte[] header_data;
            public byte[] raw_data;
        }

        public class MessageWiser3XBatteryLevel : MessageData
        {
            public uint wiser_id;
            public int battery;
        }


        public class EIMODataHeader
        {
            public byte magic_number;  // ??
            public char operation;     // ??
            public UInt32 phantom_code;
            public UInt32 data_size;
            public byte left_channel;
            public byte right_channel;
            public Double calibration;
            public byte reason;
            public byte board_version;
            public UInt16 sample_rate;
            public byte range;
            public byte axis_count;
            public byte selected_axis;
            public byte node_type;
            public byte start;
            public UInt16 seq = 0;
            public UInt32 trigger;
            public bool hasrealtime;
            public DateTime noderecordtime;
            public byte trigger_type;

            public byte chunks;
            public byte chunks_seq;

            public byte image_rows;
            public byte image_columns;
            public byte image_frames;
        }

        private struct TCPClientThreadData
        {
            public TcpClient client;
            public ProcessPhantomMessages processor;
        }

        private static Phantom _PhantomInstance = null;

        public static Phantom Instance
        {
            get
            {
                if (_PhantomInstance == null)
                {
                    _PhantomInstance = new Phantom();
                }

                return _PhantomInstance;
            }
        }

        private Phantom()
        {
        }
        public class PartialPhantomData
        {
            public byte[][] data = null;
        }

        static public Dictionary<UInt32, PartialPhantomData> partialPhantomData = new Dictionary<UInt32, PartialPhantomData>();



        public void start(ProcessPhantomMessages messageProcessor, bool receive_vibration_data)
        {
            should_stop_threads = false;
            udp_server = null;
            tcp_server = null;
            ack_packages = receive_vibration_data;

            m_messageProcessor = messageProcessor;

            Thread newThread = new Thread(UdpServer);
            newThread.Start(messageProcessor);

            if (ack_packages)
            {
                newThread = new Thread(TCPServer);
                newThread.Start(messageProcessor);
            }
        }


        public static void send_udp_command(string ip_address, string command)
        {
            byte[] command_bytes = Encoding.ASCII.GetBytes(command);

            UdpClient udp_client = new UdpClient(ip_address, udp_port);
            try
            {
                udp_client.Send(command_bytes, command_bytes.Length);
            }
            catch (Exception e)
            {
                //Console.WriteLine(e.ToString());
            }
        }

        public static void send_record_command(string ip_address, int left_channel, int right_channel, int recording_length)
        {
            String command_str = "r" + left_channel + ";" + right_channel + ";" + recording_length + "%";

            send_udp_command(ip_address, command_str);
        }

        public static void send_record_mode_command(string ip_address, RecordMode mode)
        {
            String command_str = "m" + (int)mode + "%";

            send_udp_command(ip_address, command_str);
        }

        public static void send_alarm_command(string ip_address, int channel, int value)
        {
            String command_str = "a" + channel + ";" + value + "%";

            send_udp_command(ip_address, command_str);
        }
        public static void send_einano_accel_config(string ip_address, string eimu_id, int send_interval, int sample_rate, int samples_to_get, int range, float alarm1, float alarm2, float alarm3, int alarmcheck_interval)
        {
            int alarm1_int = -1;
            int alarm2_int = -1;
            int alarm3_int = -1;

            if (alarm1 >= 0)
                alarm1_int = (int)((alarm1 * 100.0f) + 0.5f);
            if (alarm2 >= 0)
                alarm2_int = (int)((alarm2 * 100.0f) + 0.5f);
            if (alarm3 >= 0)
                alarm3_int = (int)((alarm3 * 100.0f) + 0.5f);

            String command_str = "s" + eimu_id + ";0;" +
                send_interval.ToString() + ";" +
                sample_rate.ToString() + ";" +
                samples_to_get.ToString() + ";" +
                range.ToString() + ";" +
                alarm1_int.ToString() + ";" +
                alarm2_int.ToString() + ";" +
                alarm3_int.ToString() + ";" +
                alarmcheck_interval.ToString() + ";-1;-1%";

            send_udp_command(ip_address, command_str);
        }


        public static void send_einano_accel_rpm_config(string ip_address, string eimu_id, int min_rpm, int max_rpm, int rpm_node_id)
        {
            min_rpm *= 100;
            max_rpm *= 100;

            String command_str = "s" + eimu_id + ";2;" +
                rpm_node_id.ToString() + ";" +
                min_rpm.ToString() + ";" +
                max_rpm.ToString() + ";" +
                "-1" + ";" +
                "-1" + ";" +
                "-1" + ";" +
                "-1" + ";" +
                "-1" + ";-1;-1%";

            send_udp_command(ip_address, command_str);
        }


        public void send_einano_config(string ip_address, string eimu_id, string cmd, int[] config)
        {
            String command_str = "s" + eimu_id + ";" + cmd + ";" +
                config[0] + ";" +
                config[1] + ";" +
                config[2] + ";" +
                config[3] + ";" +
                config[4] + ";" +
                config[5] + ";" +
                config[6] + ";" +
                config[7] + ";" +
                config[8] + ";" +
                config[9] + ";" + "%";

            Console.WriteLine(command_str);

            send_udp_command(ip_address, command_str);
        }

        private void TCPServer(object data)
        {
            ProcessPhantomMessages processor = (ProcessPhantomMessages)data;

            try
            {
                tcp_server = new TcpListener(IPAddress.Any, tcp_port);

                tcp_server.Start();

                while (true)
                {
                    TCPClientThreadData thread_data = new TCPClientThreadData();
                    Thread newThread = new Thread(Phantom.TCPReader);

                    TcpClient client = tcp_server.AcceptTcpClient();

                    Console.WriteLine("---------------------> ACEPTING NEW TCP CONNECT");

                    thread_data.client = client;
                    thread_data.processor = processor;

                    newThread.Start(thread_data);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                if (!should_stop_threads)
                {
                    Console.WriteLine(e.Message);
                    Environment.Exit(2);
                }
            }

            if (should_stop_threads)
                return;
        }

        private static EIMODataHeader bytesToEIMOHeader(byte[] data)
        {
            EIMODataHeader header = new EIMODataHeader();

            header.magic_number = data[0];

            if (header.magic_number == 49)
            {
                header.operation = BitConverter.ToChar(data, 1);
                header.phantom_code = BitConverter.ToUInt32(data, 2);
                header.data_size = BitConverter.ToUInt32(data, 8);
                header.left_channel = data[12];
                header.right_channel = data[13];
                header.calibration = BitConverter.ToDouble(data, 14);

                header.reason = data[22];
                header.board_version = data[23];
            }
            else if (header.magic_number == 50)
            {
                header.operation = BitConverter.ToChar(data, 1);
                header.phantom_code = BitConverter.ToUInt32(data, 2);

                header.chunks = data[6];
                header.chunks_seq = data[7];

                header.data_size = BitConverter.ToUInt32(data, 8);
                header.board_version = data[12];
                header.axis_count = (byte) (data[13] & 0x03);
                header.selected_axis = (byte) ((data[13] >> 2) & 0x03);
                header.sample_rate = BitConverter.ToUInt16(data, 14);
                header.range = data[16];
                header.calibration = BitConverter.ToSingle(data, 16);
                header.trigger_type = data[17];
                header.reason = data[20];
                header.node_type = data[21];
                header.start = data[22];
                header.seq = BitConverter.ToUInt16(data, 23);

                byte[] trigger = new byte[4];
                trigger[0] = data[25];
                trigger[1] = data[26];
                trigger[2] = data[27];
                trigger[3] = 0;
                header.trigger = BitConverter.ToUInt32(trigger, 0);

                UInt32 timestamp = BitConverter.ToUInt32(data, 28);

                if (timestamp != 0)
                {
                    header.hasrealtime = true;
                    header.noderecordtime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                    header.noderecordtime = header.noderecordtime.AddSeconds(timestamp);
                }
            }
            else if (header.magic_number == 51)
            {
                header.operation = BitConverter.ToChar(data, 1);
                header.phantom_code = BitConverter.ToUInt32(data, 2);
                header.data_size = BitConverter.ToUInt32(data, 8);
                header.board_version = data[12];
                header.reason = data[20];
                header.start = data[22];
                header.seq = BitConverter.ToUInt16(data, 23);
            }
            else if (header.magic_number == 52)
            {
                header.operation = BitConverter.ToChar(data, 1);
                header.phantom_code = BitConverter.ToUInt32(data, 2);

                header.image_columns = data[6];
                header.image_rows = data[7];

                header.image_frames = data[13];

                header.data_size = BitConverter.ToUInt32(data, 8);
                header.board_version = data[12];
                header.trigger_type = data[17];
                header.reason = data[20];
                header.node_type = data[21];
                header.start = data[22];
                header.seq = BitConverter.ToUInt16(data, 23);

                byte[] trigger = new byte[4];
                trigger[0] = data[25];
                trigger[1] = data[26];
                trigger[2] = data[27];
                trigger[3] = 0;
                header.trigger = BitConverter.ToUInt32(trigger, 0);

                UInt32 timestamp = BitConverter.ToUInt32(data, 28);

                if (timestamp != 0)
                {
                    header.hasrealtime = true;
                    header.noderecordtime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                    header.noderecordtime = header.noderecordtime.AddSeconds(timestamp);
                }
            }


            return header;
        }

        private static void calculate_rms(byte[] data, out long channel1, out long channel2)
        {
            bool is_channel1 = true;
            int processed_bytes = 0;

            channel1 = 0;
            channel2 = 0;

            for (int i = 0; i < data.Length; i += 2)
            {
                Int16 value = BitConverter.ToInt16(data, i);

                processed_bytes += 2;

                if (is_channel1)
                    channel1 = channel1 + (value * value);
                else
                    channel2 = channel2 + (value * value);

                // Flip the channel every 256 bytes
                if (processed_bytes == 256)
                {
                    is_channel1 = !is_channel1;
                    processed_bytes = 0;
                }
            }

            channel1 = (long)Math.Sqrt((channel1 / data.Length / 4));
            channel2 = (long)Math.Sqrt((channel2 / data.Length / 4));
        }

        public static Messages ParsePhantomBigMessage(byte[] headerDataBlock, byte[] dataBlockBytes, out MessageData messageData)
        {
            EIMODataHeader header = bytesToEIMOHeader(headerDataBlock);

#if false
            // This set of magic numbers means this is an EIMO header
            if (header.magic_number == 49)
            {
                MessageDataEIMO message_data = new MessageDataEIMO();

                message_data.phantom_code = header.phantom_code.ToString();
                message_data.sound_data = dataBlockBytes;

                message_data.channel1 = header.left_channel;
                message_data.channel2 = header.right_channel;
                message_data.calibration = header.calibration;
                message_data.recording_reason = (Reason)header.reason;


                calculate_rms(dataBlockBytes, out message_data.rms_channel1, out message_data.rms_channel2);

                if (header.board_version == 2)
                {
                    message_data.device = EIMO1;
                }

                Console.WriteLine("sending data for processing 1 ...");
                messageData = message_data;
                return Messages.RECEIVED_SOUNDDATA;
            }
            else 
#endif
            if (header.magic_number == 50)
            {
                if (header.axis_count == 2)
                {
                    MessageDataEIMONanoAccel message_data = new MessageDataEIMONanoAccel();
                    uint current_pos_channel = 0;
                    int byte_pos = 0;

                    message_data.channel1 = new short[dataBlockBytes.Length / 4];
                    message_data.channel2 = new short[dataBlockBytes.Length / 4];
                    message_data.channel3 = null;

                    // Separate data into its channels
                    while (byte_pos < dataBlockBytes.Length)
                    {
                        message_data.channel1[current_pos_channel] = BitConverter.ToInt16(dataBlockBytes, byte_pos);
                        message_data.channel2[current_pos_channel] = BitConverter.ToInt16(dataBlockBytes, byte_pos + 2);

                        current_pos_channel++;
                        byte_pos += 4;
                    }

                    message_data.phantom_code = header.phantom_code.ToString();

                    message_data.calibration = header.calibration;

                    // Phantom biaxial calibration was wrong, we had it at 0.45 but it was supposed to be 0.035
                    // Correct this here
                    if (header.calibration > 0.44 && header.calibration < 0.46)
                        message_data.calibration = 0.04735; // 0.045

                    if (header.reason == 0)
                        message_data.recording_reason = Reason.SCHEDULED;
                    else if (header.reason == 1)
                        message_data.recording_reason = Reason.ALARM;



                    if (header.start == 1)
                    {
                        message_data.recording_reason = Reason.REQUESTED;
                    }

                    if (header.node_type == 60)
                        message_data.device = EIMONanoAccelGP;
                    else if (header.node_type == 0)
                        message_data.device = EIMONanoBiaxial;

                    message_data.sample_rate = header.sample_rate;
                    message_data.sequence = header.seq;

                    messageData = message_data;
                    return Messages.RECEIVED_PHANTOM_ACCEL_DATA;
                }
                else
                {
                    byte[] dataBlob = dataBlockBytes;
                    bool process = true;

                    if (header.chunks > 1)
                    {
                        Debug.WriteLine("Got chunk " + header.chunks_seq.ToString());

                        if (!partialPhantomData.ContainsKey(header.phantom_code))
                        {
                            partialPhantomData[header.phantom_code] = new PartialPhantomData();
                            partialPhantomData[header.phantom_code].data = new byte[header.chunks][];
                        }

                        partialPhantomData[header.phantom_code].data[header.chunks_seq] = dataBlockBytes;

                        int data_size = 0;
                        for (int i = 0; i < partialPhantomData[header.phantom_code].data.Length; i++)
                        {
                            if (partialPhantomData[header.phantom_code].data[i] == null)
                            {
                                process = false;
                                break;
                            }

                            data_size += partialPhantomData[header.phantom_code].data[i].Length;
                        }


                        if (process)
                        {
                            Debug.WriteLine("Got all chunks\n");
                            dataBlob = new byte[data_size];
                            data_size = 0;

                            for (int i = 0; i < partialPhantomData[header.phantom_code].data.Length; i++)
                            {
                                partialPhantomData[header.phantom_code].data[i].CopyTo(dataBlob, data_size);
                                data_size += partialPhantomData[header.phantom_code].data[i].Length;
                            }
                        }
                    }

                    if (process)
                    {
                        MessageDataEIMONanoAccel message_data = new MessageDataEIMONanoAccel();
                        uint current_pos_channel = 0;
                        int byte_pos = 0;

                        if (header.axis_count == 1)
                        {
                            short[] channel_data = new short[dataBlob.Length / 2]; ;

                            while (byte_pos < dataBlob.Length)
                            {
                                channel_data[current_pos_channel] = BitConverter.ToInt16(dataBlob, byte_pos);

                                current_pos_channel++;
                                byte_pos += 2;
                            }

                            if (header.selected_axis == 0)
                                message_data.channel1 = channel_data;
                            else if (header.selected_axis == 1)
                                message_data.channel2 = channel_data;
                            else
                                message_data.channel3 = channel_data;
                        }
                        else
                        { 
                            message_data.channel1 = new short[dataBlob.Length / 6];
                            message_data.channel2 = new short[dataBlob.Length / 6];
                            message_data.channel3 = new short[dataBlob.Length / 6];

                            // Separate data into its channels
                            while (byte_pos < dataBlob.Length)
                            {
                                message_data.channel1[current_pos_channel] = BitConverter.ToInt16(dataBlob, byte_pos);
                                message_data.channel2[current_pos_channel] = BitConverter.ToInt16(dataBlob, byte_pos + 2);
                                message_data.channel3[current_pos_channel] = BitConverter.ToInt16(dataBlob, byte_pos + 4);

                                current_pos_channel++;
                                byte_pos += 6;
                            }
                        }

                        message_data.phantom_code = header.phantom_code.ToString();
                        message_data.calibration = header.range / 32768.0;
                        if (header.reason == 0)
                            message_data.recording_reason = Reason.SCHEDULED;
                        else if (header.reason == 1)
                            message_data.recording_reason = Reason.ALARM;

                        if (header.start == 1)
                        {
                            message_data.recording_reason = Reason.REQUESTED;
                        }
                        else if (header.start == 10)
                        {
                            message_data.recording_reason = Reason.SOFTRESET;
                        }

                        Debug.WriteLine("-----------------------------> " + header.start.ToString() + "<------------------------------");

                        message_data.device = EIMONanoAccelLR;
                        if (header.board_version == 2)
                            message_data.device = EIMONanoAccelHR;

                        if (header.board_version == 4)
                        {
                            message_data.device = EIMONanoUniversal;
                            message_data.battery_type = BatteryType.TWOxAAA;
                            message_data.sensor_type = SensorType.KX222;
                        }
                        else if (header.board_version == 5)
                        {
                            message_data.device = EIMONanoUniversal;
                            message_data.battery_type = BatteryType.TWOxAAA;
                            message_data.sensor_type = SensorType.KX122;
                        }
                        else if (header.board_version == 6)
                        {
                            message_data.device = EIMONanoUniversal;
                            message_data.battery_type = BatteryType.TWOxAAA;
                            message_data.sensor_type = SensorType.KX134;
                        }
                        else
                        {
                            message_data.sensor_type = (SensorType)header.board_version;
                        }


                        message_data.sample_rate = header.sample_rate;
                        message_data.sequence = header.seq;
                        message_data.trigger = (double) header.trigger / 100.0;

                        if (header.trigger_type != 42)
                        {
                            message_data.recording_reason = Reason.TRIGGERED;
                        }
                        message_data.trigger_type = (TriggerType)header.trigger_type;
                        message_data.hasrealtime = header.hasrealtime;
                        message_data.realnodetime = header.noderecordtime;
                        
                        Console.WriteLine("sending data for processing 2 ...");
                        messageData = message_data;
                        return Messages.RECEIVED_PHANTOM_ACCEL_DATA;
                    }

                    messageData = null;
                    return Messages.RECEIVED_UNKNOWMESSAGE;
                }

            }
            else if (header.magic_number == 52)
            {
                ThermalCameraImage message_data = new ThermalCameraImage();

                if (header.reason == 0)
                    message_data.recording_reason = Reason.SCHEDULED;
                else if (header.reason == 1)
                    message_data.recording_reason = Reason.ALARM;



                if (header.start == 1)
                {
                    message_data.recording_reason = Reason.REQUESTED;
                }

                message_data.phantom_code = header.phantom_code.ToString();
                message_data.sequence = header.seq;

                message_data.image_columns = header.image_columns;
                message_data.image_rows = header.image_rows;
                message_data.image_frames = header.image_frames;

                message_data.temp_data = new float[message_data.image_frames, message_data.image_columns, message_data.image_rows];


                uint line = 0;
                uint pixel = 0;
                uint frame = 0;

                for (int i = 0; i < dataBlockBytes.Length; i += 2)
                {
                    try
                    {
                        message_data.temp_data[frame, pixel, line] = ((float) BitConverter.ToUInt16(dataBlockBytes, i)) / 100.0f;
                        pixel++;
                        if (pixel == message_data.image_columns)
                        {
                            pixel = 0;
                            line++;
                        }

                        if (line == message_data.image_rows)
                        {
                            line = 0;
                            frame++;
                    }
                    }

                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                    }
                }

                message_data.device = EIMONanoThermalCamera;

                messageData = message_data;
                return Messages.RECEIVED_PHANTOM_THERMAL_IMAGE;
            }

            messageData = null;
            return Messages.RECEIVED_UNKNOWMESSAGE;
        }

        private static void TCPReader(object data)
        {
            try
            {
                TCPClientThreadData thread_data = (TCPClientThreadData)data;

                TcpClient tcp_client = thread_data.client;
                NetworkStream client_stream = tcp_client.GetStream();

                byte[] eimo_header = new byte[eimo_data_header_length];
                int bytes_read;
                int zerocount = 0;

                Console.WriteLine("Reading EIMO header...");
                bytes_read = client_stream.Read(eimo_header, 0, eimo_data_header_length);

                if (bytes_read == eimo_data_header_length)
                {
                    
                    EIMODataHeader header = bytesToEIMOHeader(eimo_header);

                    Console.WriteLine("Got EIMO header... " + header.phantom_code + " data_size = " + header.data_size + " Magic number " + header.magic_number); 


                    byte[] sound_data = new byte[header.data_size];


                    Console.WriteLine("Reading data ...");

                    bytes_read = 0;
                    while (bytes_read < header.data_size)
                    {
                        int bytes_read_now = client_stream.Read(sound_data, bytes_read, (int)header.data_size - bytes_read);

                        if (bytes_read_now == 0)
                        {
                            zerocount++;
                        }
                        else
                        {
                            zerocount = 0;
                            bytes_read += bytes_read_now;
                        }

                        if (zerocount > 20)
                        {
                            Console.WriteLine("Zerocount > 20");
                            return;
                        }
                    }
                    Console.WriteLine("Finish reading data ...");

                    MessageEIMORaw rawMessage = new MessageEIMORaw();
                    rawMessage.header_data = eimo_header;
                    rawMessage.raw_data = sound_data;
                    

                    MessageData message_data;
                    Messages message_type = ParsePhantomBigMessage(eimo_header, sound_data, out message_data);
                    thread_data.processor(message_type, message_data);

                    rawMessage.inner_message = message_type;
                    rawMessage.inner_data = message_data;
                    thread_data.processor(Messages.RECEIVED_BIGMESSAGE_BYTES, rawMessage);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }


        public void stop()
        {
            should_stop_threads = true;
            if (udp_server != null)
            {
                udp_server.Close();
            }
            if (tcp_server != null)
            {
                tcp_server.Stop();
            }
        }

        private static Int32 RecoverUnsigned(string value)
        {
            UInt32 temp1 = uint.Parse(value);
            return BitConverter.ToInt32(BitConverter.GetBytes(temp1), 0);
        }

        public static Messages ParsePhantomSmallMessage(byte [] dataBlockBytes, out MessageData messageData, string source_ip = null)
        {
            string received_string = System.Text.Encoding.ASCII.GetString(dataBlockBytes);
            string[] parameters = received_string.Split(';');
            int current_param = 0;
            /*MessageDataEIMO message_data = new MessageDataEIMO();*/
            

            if (parameters.Length > 1)
            {
                switch (Int32.Parse(parameters[current_param++]))
                {
                    case udp_message_eimosettings:
                        {
                            string nodetype = parameters[current_param + 1];
                            string cmdtype = parameters[current_param + 2];

                            if ((nodetype == "3" || nodetype == "5" || nodetype == "6" || nodetype == "7") && cmdtype == "0")
                            {
                                try
                                {
                                    MessageEIMONanoAccelSettings settings_data = new MessageEIMONanoAccelSettings();

                                    settings_data.phantom_code = parameters[current_param];
                                    current_param += 3;
                                    settings_data.send_interval = int.Parse(parameters[current_param++]);
                                    settings_data.sample_rate = int.Parse(parameters[current_param++]);
                                    settings_data.samples_to_get = int.Parse(parameters[current_param++]);
                                    settings_data.range = int.Parse(parameters[current_param++]);

                                    if (nodetype == ((int)ENANODeviceTypes.ENANO_ACCEL_UNIVERSAL).ToString())
                                        settings_data.device = EIMONanoUniversal;
                                    else
                                        settings_data.device = EIMONanoAccelLR;
                                    if (nodetype == "5")
                                    {
                                        settings_data.device = EIMONanoAccelHR;
                                    }

                                    if (parameters.Length > 10)
                                    {
                                        settings_data.alarm1 = float.Parse(parameters[current_param++]) / 100;
                                        settings_data.alarm2 = float.Parse(parameters[current_param++]) / 100;
                                        settings_data.alarm3 = float.Parse(parameters[current_param++]) / 100;

                                        settings_data.alarmcheck_interval = int.Parse(parameters[current_param++]);
                                    }

                                    settings_data.source_ip = source_ip;

                                    messageData = settings_data;
                                    return Messages.RECEIVED_PHANTOM_ACCEL_SETTINGS;
                                }
                                catch (Exception) { }
                            }
                            else if ((nodetype == "3" || nodetype == "5" || nodetype == "6" || nodetype == "7") && cmdtype == "1")
                            {
                                try
                                {
                                    MessageEIMONanoAccelState state_data = new MessageEIMONanoAccelState();

                                    state_data.phantom_code = parameters[current_param];
                                    current_param += 3;
                                    state_data.rms1 = (float)(double.Parse(parameters[current_param++]) / 100.0);
                                    state_data.rms2 = (float)(double.Parse(parameters[current_param++]) / 100.0);
                                    state_data.rms3 = (float)(double.Parse(parameters[current_param++]) / 100.0);
                                    //state_data.battery = (float)(double.Parse(parameters[current_param++]) / 100.0);

                                    UInt32 temp = UInt32.Parse(parameters[current_param++]);


                                    state_data.battery = (float)(temp & 0x0000FFFF) / 100.0f;

                                    byte bt = (byte)((temp & 0x00FF0000) >> 16);

                                    if (bt == 0)
                                        bt = 1;

                                    state_data.battery_type = (BatteryType)bt;


                                    state_data.temperature = (float)((Phantom.RecoverUnsigned(parameters[current_param++])) / 100.0);
                                    state_data.version = uint.Parse(parameters[current_param++]);
                                    state_data.device = EIMONanoAccelLR;
                                    if (nodetype == "5")
                                    {
                                        state_data.device = EIMONanoAccelHR;
                                    }

                                    if (nodetype == ((int)ENANODeviceTypes.ENANO_ACCEL_UNIVERSAL).ToString())
                                        state_data.device = EIMONanoUniversal;


                                    messageData = state_data;
                                    return Messages.RECEIVED_PHANTOM_ACCEL_STATE;
                                }
                                catch (Exception) { }
                            }
                            if ((nodetype == "3" || nodetype == "5" || nodetype == "6") && cmdtype == "2")
                            {
                                try
                                {
                                    MessageEIMONanoAccelSettings2 settings_data = new MessageEIMONanoAccelSettings2();

                                    settings_data.phantom_code = parameters[current_param];
                                    current_param += 3;
                                    settings_data.rpm_phantom_id = parameters[current_param++];
                                    settings_data.min_rpm = int.Parse(parameters[current_param++]) / 100;
                                    settings_data.max_rpm = int.Parse(parameters[current_param++]) / 100;
                                    settings_data.device = EIMONanoAccelLR;
                                    if (nodetype == "5")
                                    {
                                        settings_data.device = EIMONanoAccelHR;
                                    }

                                    messageData = settings_data;
                                    return Messages.RECEIVED_PHANTOM_ACCEL_SETTINGS2;
                                }
                                catch (Exception) { }
                            }

                            else if (parameters[current_param + 1] == "50" && parameters[current_param + 2] == "0")
                            {
                                try
                                {
                                    MessageEIMONanoAccelSettings settings_data = new MessageEIMONanoAccelSettings();

                                    settings_data.phantom_code = parameters[current_param];
                                    current_param += 3;
                                    settings_data.send_interval = int.Parse(parameters[current_param++]);
                                    settings_data.sample_rate = int.Parse(parameters[current_param++]);
                                    settings_data.samples_to_get = int.Parse(parameters[current_param++]);
                                    settings_data.range = int.Parse(parameters[current_param++]);
                                    settings_data.device = EIMONanoBiaxial;

                                    if (parameters.Length > 10)
                                    {
                                        settings_data.alarm1 = float.Parse(parameters[current_param++]) / 100;
                                        settings_data.alarm2 = float.Parse(parameters[current_param++]) / 100;
                                        settings_data.alarm3 = float.Parse(parameters[current_param++]) / 100;

                                        settings_data.alarmcheck_interval = int.Parse(parameters[current_param++]);
                                    }

                                    messageData = settings_data;
                                    return Messages.RECEIVED_PHANTOM_ACCEL_SETTINGS;
                                }
                                catch (Exception) { }
                            }
                            else if (parameters[current_param + 1] == "50" && parameters[current_param + 2] == "1")
                            {
                                try
                                {
                                    MessageEIMONanoAccelState state_data = new MessageEIMONanoAccelState();

                                    state_data.phantom_code = parameters[current_param];
                                    current_param += 3;
                                    state_data.rms1 = (float)(double.Parse(parameters[current_param++]) / 100.0);
                                    state_data.rms2 = (float)(double.Parse(parameters[current_param++]) / 100.0);
                                    state_data.rms3 = (float)(double.Parse(parameters[current_param++]) / 100.0);
                                    state_data.battery = (float)(double.Parse(parameters[current_param++]) / 100.0);
                                    state_data.temperature = (float)((RecoverUnsigned(parameters[current_param++])) / 100.0);
                                    state_data.device = EIMONanoBiaxial;

                                    messageData = state_data;
                                    return Messages.RECEIVED_PHANTOM_ACCEL_STATE;
                                }
                                catch (Exception) { }
                            }
                            else if ((nodetype == "20") && cmdtype == "0")
                            {
                                MessageDataEIMONanoTemp temp_data = new MessageDataEIMONanoTemp();

                                temp_data.phantom_code = parameters[current_param];
                                current_param += 3;

                                temp_data.type = TemperatureType.THERMOCOUPLE;
                                temp_data.temperature = (float)((RecoverUnsigned(parameters[current_param++])) / 100.0);
                                //temp_data.battery = (float)(double.Parse(parameters[current_param++]) / 100.0);


                                UInt32 temp = UInt32.Parse(parameters[current_param++]);


                                temp_data.battery = (float)(temp & 0x0000FFFF) / 100.0f;

                                byte bt = (byte)((temp & 0x00FF0000) >> 16);

                                if (bt == 0)
                                    bt = 1;

                                temp_data.battery_type = (BatteryType)bt;

                                temp_data.module_temperature = (float)((RecoverUnsigned(parameters[current_param++])) / 100.0);
                                temp_data.device = EIMONanoTempTP;

                                temp_data.version = UInt32.Parse(parameters[current_param++]);
                                temp_data.seq = UInt32.Parse(parameters[current_param++]);
                                temp_data.temperature2 = (float)((RecoverUnsigned(parameters[current_param++])) / 100.0);
                                temp_data.temperature3 = (float)((RecoverUnsigned(parameters[current_param++])) / 100.0);

                                messageData = temp_data;
                                return Messages.RECEIVED_PHANTOM_TEMP_DATA;
                            }
                            else if ((nodetype == "21" && cmdtype == "0") ||
                                        (nodetype == "7" && cmdtype == "4"))
                            {
                                MessageDataEIMONanoTemp temp_data = new MessageDataEIMONanoTemp();

                                temp_data.phantom_code = parameters[current_param];
                                current_param += 3;

                                temp_data.type = TemperatureType.THERMOCOUPLE;
                                string tempstr = parameters[current_param++];
                                UInt32 temp = UInt32.Parse(tempstr);
                                if (temp == 0xFFFFFFFF)
                                {
                                    temp_data.temp1fault = true;
                                    temp_data.temperature = 1000000;
                                }
                                else
                                    temp_data.temperature = (float)(RecoverUnsigned(tempstr) / 100.0);
                                //temp_data.battery = (float)(double.Parse(parameters[current_param++]) / 100.0);


                                temp = UInt32.Parse(parameters[current_param++]);


                                temp_data.battery = (float)(temp & 0x0000FFFF) / 100.0f;

                                byte bt = (byte)((temp & 0x00FF0000) >> 16);

                                if (bt == 0)
                                    bt = 1;

                                temp_data.battery_type = (BatteryType)bt;

                                temp_data.module_temperature = (float)((Phantom.RecoverUnsigned(parameters[current_param++])) / 100.0);
                                if (nodetype == ((int)ENANODeviceTypes.ENANO_ACCEL_UNIVERSAL).ToString())
                                    temp_data.device = EIMONanoUniversal;
                                else
                                    temp_data.device = EIMONanoTempTPV2;

                                temp_data.version = UInt32.Parse(parameters[current_param++]);
                                temp_data.seq = UInt32.Parse(parameters[current_param++]);

                                tempstr = parameters[current_param++];
                                temp = UInt32.Parse(tempstr);
                                if (temp == 0xFFFFFFFF)
                                {
                                    temp_data.temp2fault = true;
                                    temp_data.temperature2 = 1000000;
                                }
                                else
                                    temp_data.temperature2 = (float)(Phantom.RecoverUnsigned(tempstr) / 100.0);

                                tempstr = parameters[current_param++];
                                temp = UInt32.Parse(tempstr);
                                if (temp == 0xFFFFFFFF)
                                {
                                    temp_data.temp3fault = true;
                                    temp_data.temperature3 = 1000000;
                                }
                                else
                                    temp_data.temperature3 = (float)(Phantom.RecoverUnsigned(tempstr) / 100.0);

                                messageData = temp_data;
                                return Messages.RECEIVED_PHANTOM_TEMP_DATA;
                            }
                            else if (nodetype == "25" && cmdtype == "0")
                            {
                                MessageDataEIMONanoTemp temp_data = new MessageDataEIMONanoTemp();

                                temp_data.phantom_code = parameters[current_param];
                                current_param += 3;

                                temp_data.type = TemperatureType.INFRARED;
                                temp_data.temperature = (float)((Phantom.RecoverUnsigned(parameters[current_param++])) / 100.0);
                                temp_data.battery = (float)(double.Parse(parameters[current_param++]) / 100.0);
                                temp_data.module_temperature = (float)((Phantom.RecoverUnsigned(parameters[current_param++])) / 100.0);
                                temp_data.ambient_temperature = (float)((Phantom.RecoverUnsigned(parameters[current_param++])) / 100.0);
                                temp_data.device = EIMONanoTempNC;

                                temp_data.version = UInt32.Parse(parameters[current_param++]);
                                temp_data.seq = UInt32.Parse(parameters[current_param++]);


                                messageData = temp_data;
                                return Messages.RECEIVED_PHANTOM_TEMP_DATA;
                            }
                            else if (parameters[current_param + 1] == "26" && parameters[current_param + 2] == "0")
                            {
                                MessageData420 temp_data = new MessageData420();

                                temp_data.phantom_code = parameters[current_param];
                                current_param += 3;

                                temp_data.channel1_voltage = (float)((Int32.Parse(parameters[current_param++])) / 1000.0);
                                //temp_data.battery = (float)(double.Parse(parameters[current_param++]) / 100.0);

                                UInt32 temp = UInt32.Parse(parameters[current_param++]);


                                temp_data.battery = (float)(temp & 0x0000FFFF) / 100.0f;

                                byte bt = (byte)((temp & 0x00FF0000) >> 16);

                                if (bt == 0)
                                    bt = 1;

                                temp_data.battery_type = (BatteryType)bt;

                                temp_data.module_temperature = (float)((Phantom.RecoverUnsigned(parameters[current_param++])) / 100.0);
                                temp_data.channel2_voltage = (float)((Int32.Parse(parameters[current_param++])) / 1000.0);
                                temp_data.channel3_voltage = (float)((Int32.Parse(parameters[current_param++])) / 1000.0);
                                temp_data.channel4_voltage = (float)((Int32.Parse(parameters[current_param++])) / 1000.0);
                                temp_data.device = EIMONano420;

                                temp_data.version = UInt32.Parse(parameters[current_param++]);
                                temp_data.seq = UInt32.Parse(parameters[current_param++]);

                                messageData = temp_data;
                                return Messages.RECEIVED_PHANTOM_420_DATA;
                            }
                            else if (parameters[current_param + 1] == "27" && parameters[current_param + 2] == "0")
                            {
                                MessageVoltimeter temp_data = new MessageVoltimeter();

                                temp_data.phantom_code = parameters[current_param];
                                current_param += 3;

                                temp_data.channel1_voltage = (float)((Int32.Parse(parameters[current_param++])) / 1000.0);
                                //temp_data.battery = (float)(double.Parse(parameters[current_param++]) / 100.0);

                                UInt32 temp = UInt32.Parse(parameters[current_param++]);


                                temp_data.battery = (float)(temp & 0x0000FFFF) / 100.0f;

                                byte bt = (byte)((temp & 0x00FF0000) >> 16);

                                if (bt == 0)
                                    bt = 1;

                                temp_data.battery_type = (BatteryType)bt;

                                temp_data.module_temperature = (float)((Phantom.RecoverUnsigned(parameters[current_param++])) / 100.0);
                                temp_data.channel2_voltage = (float)((Int32.Parse(parameters[current_param++])) / 1000.0);
                                temp_data.channel3_voltage = (float)((Int32.Parse(parameters[current_param++])) / 1000.0);
                                temp_data.channel4_voltage = (float)((Int32.Parse(parameters[current_param++])) / 1000.0);
                                temp_data.device = EIMONanoVoltimeter;

                                temp_data.version = UInt32.Parse(parameters[current_param++]);
                                temp_data.seq = UInt32.Parse(parameters[current_param++]);

                                messageData = temp_data;
                                return Messages.RECEIVED_PHANTOM_VOLTAGE_DATA;
                            }
                            else if ((nodetype == "30") && cmdtype == "0")
                            {
                                MessageEIMONanoCurrentData temp_data = new MessageEIMONanoCurrentData();

                                temp_data.phantom_code = parameters[current_param];
                                current_param += 3;


                                temp_data.battery = (float)(double.Parse(parameters[current_param++]) / 100.0);
                                temp_data.module_temperature = (float)(double.Parse(parameters[current_param++]) / 100.0);
                                temp_data.current1 = (float)(double.Parse(parameters[current_param++]) / 100.0);
                                temp_data.current2 = (float)(double.Parse(parameters[current_param++]) / 100.0);
                                temp_data.current3 = (float)(double.Parse(parameters[current_param++]) / 100.0);
                                temp_data.device = EIMONanoCurrent;

                                temp_data.version = UInt32.Parse(parameters[current_param++]);
                                temp_data.seq = UInt32.Parse(parameters[current_param++]);

                                messageData = temp_data;
                                return Messages.RECEIVED_PHANTOM_CURRENT_DATA;
                            }
                            else if ((nodetype == "32" && cmdtype == "0") ||
                                     (nodetype == "7" && cmdtype == "2")) // Phantom universal
                            {
                                MessageEIMONanoCurrentDataV2 temp_data = new MessageEIMONanoCurrentDataV2();

                                temp_data.phantom_code = parameters[current_param];
                                current_param += 3;

                                UInt32 temp = UInt32.Parse(parameters[current_param++]);


                                temp_data.battery = (float)(temp & 0x0000FFFF) / 100.0f;

                                byte bt = (byte) ((temp & 0x00FF0000) >> 16);

                                if (bt == 0)
                                    bt = 1;

                                temp_data.battery_type = (BatteryType) bt;


                                temp_data.module_temperature = (float)(double.Parse(parameters[current_param++]) / 100.0);
                                temp_data.version = UInt32.Parse(parameters[current_param++]);
                                temp_data.seq = UInt32.Parse(parameters[current_param++]);

                                UInt32 b1 = UInt32.Parse(parameters[current_param++]);
                                UInt32 b2 = UInt32.Parse(parameters[current_param++]);
                                UInt32 b3 = UInt32.Parse(parameters[current_param++]);
                                UInt32 b4 = UInt32.Parse(parameters[current_param++]);
                                UInt32 b5 = UInt32.Parse(parameters[current_param++]);
                                UInt32 b6 = UInt32.Parse(parameters[current_param++]);

                                temp_data.avg_current1 = (float) (b1 >> 16) / 100.0f;
                                temp_data.avg_current2 = (float) (b1 & 0x0000FFFF) / 100.0f;

                                temp_data.avg_current3 = (float)(b2 >> 16) / 100.0f;
                                temp_data.avg_current4 = (float)(b2 & 0x0000FFFF) / 100.0f;

                                temp_data.min_current1 = (float)(b3 >> 16) / 100.0f;
                                temp_data.min_current2 = (float)(b3 & 0x0000FFFF) / 100.0f;

                                temp_data.min_current3 = (float)(b4 >> 16) / 100.0f;
                                temp_data.min_current4 = (float)(b4 & 0x0000FFFF) / 100.0f;

                                temp_data.max_current1 = (float)(b5 >> 16) / 100.0f;
                                temp_data.max_current2 = (float)(b5 & 0x0000FFFF) / 100.0f;

                                temp_data.max_current3 = (float)(b6 >> 16) / 100.0f;
                                temp_data.max_current4 = (float)(b6 & 0x0000FFFF) / 100.0f;

                                //temp_data.current1 = (float)(double.Parse(parameters[current_param++]) / 100.0);
                                //temp_data.current2 = (float)(double.Parse(parameters[current_param++]) / 100.0);
                                //temp_data.current3 = (float)(double.Parse(parameters[current_param++]) / 100.0);
                                if (nodetype == ((int)ENANODeviceTypes.ENANO_ACCEL_UNIVERSAL).ToString())
                                    temp_data.device = EIMONanoUniversal;
                                else
                                    temp_data.device = EIMONanoCurrentV2;

                                messageData = temp_data;
                                return Messages.RECEIVED_PHANTOM_CURRENT_DATAV2;
                            }
                            else if ((nodetype == "32" && cmdtype == "1") ||
                                     (nodetype == "7" && cmdtype == "3"))
                            {
                                MessageEIMONanoAccumulatedCurrentData temp_data = new MessageEIMONanoAccumulatedCurrentData();

                                temp_data.phantom_code = parameters[current_param];
                                current_param += 3;


                                temp_data.accumulated_current1 = (float)(double.Parse(parameters[current_param++]) / 100.0);
                                temp_data.accumulated_current2 = (float)(double.Parse(parameters[current_param++]) / 100.0);
                                temp_data.accumulated_current3 = (float)(double.Parse(parameters[current_param++]) / 100.0);
                                temp_data.accumulated_current4 = (float)(double.Parse(parameters[current_param++]) / 100.0);

                                UInt32 start_date = UInt32.Parse(parameters[current_param++]);
                                temp_data.startPeriod = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                                temp_data.startPeriod = temp_data.startPeriod.AddSeconds(start_date);

                                UInt32 end_date = UInt32.Parse(parameters[current_param++]);
                                temp_data.endPeriod = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                                temp_data.endPeriod = temp_data.endPeriod.AddSeconds(end_date);

                                if (nodetype == ((int)ENANODeviceTypes.ENANO_ACCEL_UNIVERSAL).ToString())
                                    temp_data.device = EIMONanoUniversal;
                                else
                                    temp_data.device = EIMONanoCurrentV2;

                                messageData = temp_data;
                                return Messages.RECEIVED_PHANTOM_ACCUMULATED_CURRENT_DATA;
                            }
                            else if (parameters[current_param + 1] == "31" && parameters[current_param + 2] == "0")
                            {
                                MessageEIMONanoDigitalInData temp_data = new MessageEIMONanoDigitalInData();

                                temp_data.phantom_code = parameters[current_param];
                                current_param += 3;


                                UInt32 temp = UInt32.Parse(parameters[current_param++]);
                                temp_data.battery = (float)(temp & 0x0000FFFF) / 100.0f;

                                byte bt = (byte)((temp & 0x00FF0000) >> 16);

                                if (bt == 0)
                                    bt = 1;

                                temp_data.battery_type = (BatteryType)bt;


                                //temp_data.battery = (float)(double.Parse(parameters[current_param++]) / 100.0);
                                temp_data.module_temperature = (float)(double.Parse(parameters[current_param++]) / 100.0);

                                UInt32 inputs = UInt32.Parse(parameters[current_param++]);

                                temp_data.input1 = (inputs & 0x01) == 0x01;
                                temp_data.input2 = (inputs & 0x02) == 0x02;
                                temp_data.input3 = (inputs & 0x04) == 0x04;
                                temp_data.input4 = (inputs & 0x08) == 0x08;

                                temp_data.device = EIMONanoDigitalIn;

                                temp_data.version = UInt32.Parse(parameters[current_param++]);
                                temp_data.seq = UInt32.Parse(parameters[current_param++]);

                                messageData = temp_data;
                                return Messages.RECEIVED_PHANTOM_DIGITAL_INPUTS_DATA;
                            }

                            else if (parameters[current_param + 1] == "40" && parameters[current_param + 2] == "0")
                            {
                                MessageEIMONanoRPMData temp_data = new MessageEIMONanoRPMData();

                                temp_data.phantom_code = parameters[current_param];
                                current_param += 3;

                                temp_data.trigger = (float)(double.Parse(parameters[current_param++]) / 100.0);
                                //temp_data.battery = (float)(double.Parse(parameters[current_param++]) / 100.0);


                                ////////////////////
                                UInt32 temp = UInt32.Parse(parameters[current_param++]);

                                temp_data.battery = (float)(temp & 0x0000FFFF) / 100.0f;

                                byte bt = (byte)((temp & 0x00FF0000) >> 16);

                                if (bt == 0)
                                    bt = 1;

                                temp_data.battery_type = (BatteryType)bt;
                                ////////////////////


                                temp_data.module_temperature = (float)(double.Parse(parameters[current_param++]) / 100.0);
                                temp_data.device = EIMONanoRPM;

                                temp_data.version = UInt32.Parse(parameters[current_param++]);
                                temp_data.seq = UInt32.Parse(parameters[current_param++]);

                                messageData = temp_data;
                                return Messages.RECEIVED_PHANTOM_RPM_DATA;
                            }
                            else if (parameters[current_param + 1] == "60" && parameters[current_param + 2] == "0")
                            {
                                try
                                {
                                    MessageEIMONanoGPSettings settings_data = new MessageEIMONanoGPSettings();

                                    settings_data.phantom_code = parameters[current_param];
                                    current_param += 3;
                                    settings_data.send_interval = int.Parse(parameters[current_param++]);
                                    settings_data.sample_rate = int.Parse(parameters[current_param++]);
                                    settings_data.samples_to_get = int.Parse(parameters[current_param++]);
                                    settings_data.range = int.Parse(parameters[current_param++]);
                                    settings_data.device = EIMONanoAccelGP;

                                    if (parameters.Length > 10)
                                    {
                                        settings_data.alarm1 = float.Parse(parameters[current_param++]) / 100;
                                        settings_data.alarm2 = float.Parse(parameters[current_param++]) / 100;

                                        settings_data.alarmcheck_interval = int.Parse(parameters[current_param++]);
                                    }

                                    messageData = settings_data;
                                    return Messages.RECEIVED_PHANTOM_GP_SETTINGS;
                                }
                                catch (Exception) { }
                            }
                            else if (parameters[current_param + 1] == "60" && parameters[current_param + 2] == "1")
                            {
                                try
                                {
                                    MessageEIMONanoGPAccelState state_data = new MessageEIMONanoGPAccelState();

                                    state_data.phantom_code = parameters[current_param];
                                    current_param += 3;
                                    state_data.rms1 = (float)(double.Parse(parameters[current_param++]) / 100.0);
                                    state_data.rms2 = (float)(double.Parse(parameters[current_param++]) / 100.0);

                                    state_data.vpp_min1 = (float)((Phantom.RecoverUnsigned(parameters[current_param++])) / 100.0);

                                    state_data.battery = (float)(double.Parse(parameters[current_param++]) / 100.0);
                                    state_data.temperature = (float)((Phantom.RecoverUnsigned(parameters[current_param++])) / 100.0);

                                    state_data.vpp_max1 = (float)((Phantom.RecoverUnsigned(parameters[current_param++])) / 100.0);
                                    state_data.vpp_min2 = (float)((Phantom.RecoverUnsigned(parameters[current_param++])) / 100.0);
                                    state_data.vpp_max2 = (float)((Phantom.RecoverUnsigned(parameters[current_param++])) / 100.0);

                                    float offset1 = (float)((Phantom.RecoverUnsigned(parameters[current_param++])) / 100.0);
                                    float offset2 = (float)((Phantom.RecoverUnsigned(parameters[current_param++])) / 100.0);

                                    state_data.vpp_min01 = state_data.vpp_min1 - offset1;
                                    state_data.vpp_max01 = state_data.vpp_max1 - offset1;

                                    state_data.vpp_min02 = state_data.vpp_min2 - offset2;
                                    state_data.vpp_max02 = state_data.vpp_max2 - offset2;

                                    state_data.device = EIMONanoAccelGP;


                                    messageData = state_data;
                                    return Messages.RECEIVED_PHANTOM_GP_STATE;
                                }
                                catch (Exception) { }
                            }
                            else if (parameters[current_param + 1] == "10" && parameters[current_param + 2] == "0")
                            {
                                try
                                {
                                    ThermalCameraData state_data = new ThermalCameraData();

                                    state_data.phantom_code = parameters[current_param];
                                    current_param += 3;
                                    state_data.average_frame_temperature = (float)(double.Parse(parameters[current_param++]) / 100.0);
                                    state_data.average_roi_temperature = (float)(double.Parse(parameters[current_param++]) / 100.0);

                                    state_data.battery_type = BatteryType.TWOxAAA;
                                    state_data.battery = (float)((Phantom.RecoverUnsigned(parameters[current_param++])) / 100.0);

                                    state_data.module_temperature = (float)(double.Parse(parameters[current_param++]) / 100.0);
                                    state_data.version = uint.Parse(parameters[current_param++]);

                                    state_data.device = EIMONanoThermalCamera;

                                    messageData = state_data;
                                    return Messages.RECEIVED_PHANTOM_THERMAL_DATA;
                                }
                                catch (Exception) { }
                            }
                            else if (parameters[current_param + 1] == "253" && parameters[current_param + 2] == "1")
                            {
                                MessageEIMORSSIData rssi_data = new MessageEIMORSSIData();

                                rssi_data.phantom_code = parameters[current_param];
                                current_param += 3;

                                string string_data = parameters[current_param++];

                                UInt32 temp1 = UInt32.Parse(string_data);


                                byte[] buffer = BitConverter.GetBytes(temp1);
                                rssi_data.min_rssi = unchecked((sbyte)buffer[3]);
                                rssi_data.max_rssi = unchecked((sbyte)buffer[2]);

                                rssi_data.min_channel = buffer[1];
                                rssi_data.max_channel = buffer[0];

                                messageData = rssi_data;
                                return Messages.RECEIVED_PHANTOM_RSSI_DATA;

                            }
                        }
                        break;
                    case udp_message_eimotemperature:
                        {
                            MessageDataEIMONanoTemp temp_data = new MessageDataEIMONanoTemp();

                            temp_data.device = Phantom.EIMONanoTempTP;
                            temp_data.recording_reason = Reason.SCHEDULED;
                            temp_data.phantom_code = parameters[2];
                            temp_data.temperature = float.Parse(parameters[3]) / 100.0f;

                            messageData = temp_data;
                            return Messages.RECEIVED_PHANTOM_TEMP_DATA;
                        }
                    case udp_message_eimoinfo:
                        bool wellformed = false;

                        if (parameters.Length >= 4)
                        {
                            MessageDataIDInfo message_data_id = new MessageDataIDInfo();

                            string ip_address = parameters[current_param++];
                            string phantom_code = parameters[current_param++];
                            string device = parameters[current_param++];

                            message_data_id.ip_address = ip_address;
                            message_data_id.phantom_code = phantom_code;

                            if (parameters.Length >= 5)
                            {
                                string device_type_str = parameters[current_param++];
                                ENANODeviceTypes devicetype = (ENANODeviceTypes)int.Parse(device_type_str);

                                if (device != null && device == "EINANOGW")
                                {
                                    message_data_id.device = "EINANOGW";
                                    message_data_id.wf_version = device_type_str;
                                    message_data_id.bt_version = "0";

                                    if (parameters.Length >= 6)
                                    {
                                        message_data_id.bt_version = parameters[current_param++];
                                    }
                                }
                                else
                                {
                                    switch (devicetype)
                                    {
                                        case ENANODeviceTypes.ENANO_ACCEL_3X_HR:
                                        case ENANODeviceTypes.ENANO_ACCEL_3X_HR_V2:
                                            message_data_id.device = EIMONanoAccelHR;
                                            break;
                                        case ENANODeviceTypes.ENANO_BIAXIAL:
                                            message_data_id.device = EIMONanoBiaxial;
                                            break;
                                        case ENANODeviceTypes.ENANO_ACCEL_3X_LR:
                                            message_data_id.device = EIMONanoAccelLR;
                                            break;
                                        case ENANODeviceTypes.ENANO_TEMPERATURE_CONTACT:
                                            message_data_id.device = EIMONanoTempTP;
                                            break;
                                        case ENANODeviceTypes.ENANO_TEMPERATURE_CONTACTV2:
                                            message_data_id.device = EIMONanoTempTPV2;
                                            break;
                                        case ENANODeviceTypes.ENANO_TEMPERATURE_INFRARED:
                                            message_data_id.device = EIMONanoTempNC;
                                            break;
                                        case ENANODeviceTypes.ENANO_CURRENT:
                                            message_data_id.device = EIMONanoCurrent;
                                            break;
                                        case ENANODeviceTypes.ENANO_CURRENT_V2:
                                            message_data_id.device = EIMONanoCurrentV2;
                                            break;
                                        case ENANODeviceTypes.ENANO_RPM:
                                            message_data_id.device = EIMONanoRPM;
                                            break;
                                        case ENANODeviceTypes.ENANO_GP:
                                            message_data_id.device = EIMONanoAccelGP;
                                            break;
                                        case ENANODeviceTypes.ENANO_420:
                                            message_data_id.device = EIMONano420;
                                            break;
                                        case ENANODeviceTypes.ENANO_VOLTIMETER:
                                            message_data_id.device = EIMONanoVoltimeter;
                                            break;
                                        case ENANODeviceTypes.ENANO_DIGITA_INPUTS:
                                            message_data_id.device = EIMONanoDigitalIn;
                                            break;
                                        case ENANODeviceTypes.ENANO_ACCEL_UNIVERSAL:
                                            message_data_id.device = EIMONanoUniversal;
                                            break;
                                        case ENANODeviceTypes.ENANO_THERMAL_CAMERA:
                                            message_data_id.device = EIMONanoThermalCamera;
                                            break;

                                    }
                                }
                            }
                            else
                            {
                                message_data_id.device = device;
                            }

                            messageData = message_data_id;
                            return Messages.RECEIVED_ID_INFO;
                        }
                        break;
                    default:
                        break;
                }
            }

            messageData = null;
            return Messages.RECEIVED_UNKNOWMESSAGE;
        }

        private void UdpServer(object data)
        {
            ProcessPhantomMessages processor = (ProcessPhantomMessages)data;
            //IPAddress multicastaddress = IPAddress.Parse(multicast_ip_address);
            IPEndPoint ip_endpoint = new IPEndPoint(IPAddress.Any, udp_port);


            udp_server = new UdpClient(AddressFamily.InterNetwork);
            udp_server.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            
            //udp_server.JoinMulticastGroup(multicastaddress);
            /*
            try { 
                
            }
            catch (SocketException e)
            {
                File.WriteAllText("C:\\error.txt", "c" + e.ToString() + " error code " + e.ErrorCode.ToString());
            }*/

            udp_server.Client.Bind(ip_endpoint);


            while (true)
            {
                byte[] received_data = null;

                try
                {
                    received_data = udp_server.Receive(ref ip_endpoint);
                }
                catch (Exception)
                {
                    if (should_stop_threads)
                        return;

                    continue;
                };

                if (should_stop_threads)
                    return;


                MessageEIMORaw rawMessage = new MessageEIMORaw();
                rawMessage.raw_data = received_data;

                MessageData messageData;
                Messages messageType = ParsePhantomSmallMessage(received_data, out messageData, ip_endpoint.Address.ToString());

                if (messageType == Messages.RECEIVED_ID_INFO)
                {
                    MessageDataIDInfo info_data = (MessageDataIDInfo)messageData;

                    if (ack_packages && info_data.device == "EINANOGW")
                    {
                        UInt32 unixTimestamp = (UInt32)((DateTime.Now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds);
                        unixTimestamp++;

                        send_udp_command(info_data.ip_address, "o" + unixTimestamp.ToString() + "%"); ;
                    }
                }

                processor(messageType, messageData);

                rawMessage.inner_message = messageType;
                rawMessage.inner_data = messageData;
                processor(Messages.RECEVIED_SMALLMESSAGE_BYTES, rawMessage);
            }
        }
    }
}
