using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using MySql.Data.MySqlClient;
using Google.Protobuf.WellKnownTypes;
using System.Xml;
using MySqlX.XDevAPI.Common;
using dotenv.net;




namespace Capturador
{
    class PhantomLibExample
    {
        //static readonly string connectionString = "Server=database-1.cz4c5r0xyxmz.sa-east-1.rds.amazonaws.com;Port=3306;Database=vitech;Uid=root;Password=6FhkSCMirUL7a9khVALCbaLYFpWvJwn3v";
        static S3connection s3client;
        static SqlConnection dbClient;

        static ConcurrentQueue<object[]> messagesQueue = new ConcurrentQueue<object[]>();
        static Dictionary<string, string> phantom_to_ip = new Dictionary<string, string>();

        static void processMessages(Phantom.Messages message, Phantom.MessageData data)
        {
            object[] parameters = new object[3];
            parameters[0] = message;
            parameters[1] = data;
            parameters[2] = DateTime.Now;

            messagesQueue.Enqueue(parameters);

            if (messagesQueue.Count > 100)
            {
                object[] dummy;
                messagesQueue.TryDequeue(out dummy);
            }
        }

        static void Main(string[] args)
        {
            DotEnv.Load(options: new DotEnvOptions(ignoreExceptions: true, envFilePaths: new[] { "./.env", "./../../.env", "./../../../.env" }));
            s3client = new S3connection(Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID"),
                Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY"), Environment.GetEnvironmentVariable("AWS_BUCKET_NAME"));
            dbClient = new SqlConnection(Environment.GetEnvironmentVariable("SQLHOST"), Environment.GetEnvironmentVariable("SQLDB"),
                Environment.GetEnvironmentVariable("SQLUSER"), Environment.GetEnvironmentVariable("SQLPASS"));


            Phantom.Instance.start(new Phantom.ProcessPhantomMessages(processMessages), true);

            while (true)
            {
                object[] parameters;
                if (messagesQueue.TryDequeue(out parameters))
                {
                    Phantom.Messages message = (Phantom.Messages)parameters[0];
                    Phantom.MessageData generic_data = (Phantom.MessageData)parameters[1];
                    DateTime message_time = (DateTime)parameters[2];

                    switch (message)
                    {
                        case Phantom.Messages.RECEIVED_ID_INFO: // aqui se recibe id de sensor (se imprime en pantalla)
                            {
                                Phantom.MessageDataIDInfo data = (Phantom.MessageDataIDInfo)generic_data;

                                phantom_to_ip[data.phantom_code] = data.ip_address;

                                Console.WriteLine("Id from " + data.phantom_code + " " + data.ip_address);

                                break;
                            }
                        case Phantom.Messages.RECEIVED_PHANTOM_ACCEL_DATA: // Aquí se recibe la vibración.
                            {

                                Phantom.MessageDataEIMONanoAccel data = (Phantom.MessageDataEIMONanoAccel)generic_data;

                                String[] contenido = new string[1];

                                string nombre_archivo;

                                // al realizar una busqueda, verificar que existe el id de sensor, en caso contrario crearlo
                                int id_punto = dbClient.SelectSensor(data.phantom_code);

                                Console.WriteLine(id_punto);

                                if (id_punto == 0)
                                {
                                    return;
                                }



                                // Para Monoaxial X
                                else if (data.channel2 == null && data.channel3 == null)
                                {
                                    String[] line1 = new string[data.channel1.Length + 1];

                                    for (int i = 0; i < data.channel1.Length; i++)
                                    {
                                        line1[i] = (data.channel1[i] * data.calibration).ToString();
                                    }
                                    line1[data.channel1.Length] = data.sample_rate.ToString(); //Contenido de Sample rate en el último elemento.

                                    nombre_archivo = data.realnodetime.ToString().Replace('/', '-');
                                    nombre_archivo = nombre_archivo.Replace(':', ',');

                                    System.IO.File.WriteAllLines(@"C:\Ruta\X" + " - " + data.phantom_code + " " + nombre_archivo + ".csv", line1);

                                    s3client.UploadDataAsync("X - " + data.phantom_code + nombre_archivo + ".csv", line1);

                                    nombre_archivo = "X - " + data.phantom_code + nombre_archivo + ".csv";

                                    dbClient.UploadReadingMonoAxial(data, nombre_archivo, id_punto, "X");

                                }

                                // Para Monoaxial Y
                                else if (data.channel1 == null && data.channel3 == null)
                                {
                                    String[] line2 = new string[data.channel2.Length + 1];

                                    for (int i = 0; i < data.channel2.Length; i++)
                                    {
                                        line2[i] = (data.channel2[i] * data.calibration).ToString();
                                    }
                                    line2[data.channel2.Length] = data.sample_rate.ToString(); //Contenido de Sample rate en el último elemento.

                                    nombre_archivo = data.realnodetime.ToString().Replace('/', '-');
                                    nombre_archivo = nombre_archivo.Replace(':', ',');

                                    System.IO.File.WriteAllLines(@"C:\Ruta\Y" + " - " + data.phantom_code + " " + nombre_archivo + ".csv", line2);
                                    s3client.UploadDataAsync("Y - " + data.phantom_code + nombre_archivo + ".csv", line2);

                                    nombre_archivo = "Y - " + data.phantom_code + nombre_archivo + ".csv";

                                    dbClient.UploadReadingMonoAxial(data, nombre_archivo, id_punto, "Y");

                                }

                                // Para Monoaxial Z
                                else if (data.channel1 == null && data.channel2 == null)
                                {
                                    String[] line3 = new string[data.channel3.Length + 1];

                                    for (int i = 0; i < data.channel3.Length; i++)
                                    {
                                        line3[i] = (data.channel3[i] * data.calibration).ToString();
                                    }
                                    line3[data.channel3.Length] = data.sample_rate.ToString(); //Contenido de Sample rate en el último elemento.

                                    nombre_archivo = data.realnodetime.ToString().Replace('/', '-');
                                    nombre_archivo = nombre_archivo.Replace(':', ',');

                                    System.IO.File.WriteAllLines(@"C:\Ruta\Z" + " - " + data.phantom_code + " " + nombre_archivo + ".csv", line3);
                                    s3client.UploadDataAsync("Z - " + data.phantom_code + nombre_archivo + ".csv", line3); 

                                    nombre_archivo = data.phantom_code + nombre_archivo;

                                    dbClient.UploadReadingMonoAxial(data, nombre_archivo, id_punto, "Z");

                                }

                                // Para triaxial
                                else
                                {
                                    String[] line1 = new string[data.channel1.Length];
                                    String[] line2 = new string[data.channel2.Length];
                                    String[] line3 = new string[data.channel3.Length];


                                    for (int i = 0; i < data.channel1.Length; i++)
                                    {
                                        line1[i] = (data.channel1[i] * data.calibration).ToString();
                                        line2[i] = (data.channel2[i] * data.calibration).ToString();
                                        line3[i] = (data.channel3[i] * data.calibration).ToString();
                                    }

                                    contenido[0] = data.sample_rate.ToString(); //Contenido de Sample rate.csv

                                    nombre_archivo = data.realnodetime.ToString().Replace('/', '-');
                                    nombre_archivo = nombre_archivo.Replace(':', ',');


                                    System.IO.File.WriteAllLines(@"C:\Ruta\X" + " - " + data.phantom_code + nombre_archivo + ".csv", line1);
                                    s3client.UploadDataAsync("X - " + data.phantom_code + nombre_archivo + ".csv", line1);

                                    System.IO.File.WriteAllLines(@"C:\Ruta\Y" + " - " + data.phantom_code + nombre_archivo + ".csv", line2);
                                    s3client.UploadDataAsync("Y - " + data.phantom_code + nombre_archivo + ".csv", line2);

                                    System.IO.File.WriteAllLines(@"C:\Ruta\Z" + " - " + data.phantom_code + nombre_archivo + ".csv", line3);
                                    s3client.UploadDataAsync("Z - " + data.phantom_code + nombre_archivo + ".csv", line3);


                                    dbClient.UploadReadingMultiAxial(data, nombre_archivo, id_punto);
                                }



                                Console.WriteLine("Data from " + data.phantom_code); //multiplicar cada canal por la calibración resulta en el valor de vibración en G (9.8m/s²).


                                break;

                            }
                        case Phantom.Messages.RECEIVED_PHANTOM_TEMP_DATA: // se recibe la tempreratura
                            {
                                Phantom.MessageDataEIMONanoTemp data = (Phantom.MessageDataEIMONanoTemp)generic_data;

                                if (data.type == Phantom.TemperatureType.INFRARED)
                                {
                                    Console.WriteLine("TEmperature from infrarred " + data.phantom_code + " Object Temp " + data.temperature.ToString() +
                                        " Ambient Temp " + data.ambient_temperature.ToString() +
                                        "C Battery " + data.battery.ToString() + " Module Temperature: " + data.module_temperature.ToString() + " (" + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + ")\n");
                                }
                                else
                                {
                                    Console.WriteLine("TEmperature from termocopule " + data.phantom_code + " " + data.temperature.ToString() +
                                        "C Battery " + data.battery.ToString() + " Module Temperature: " + data.module_temperature.ToString() + " (" + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + ")\n");
                                }

                                break;
                            }
                        case Phantom.Messages.RECEIVED_PHANTOM_ACCEL_SETTINGS:
                            {
                                Phantom.MessageEIMONanoAccelSettings data = (Phantom.MessageEIMONanoAccelSettings)generic_data;

                                Console.WriteLine("Settings from " + data.phantom_code + " send interval: " + data.send_interval.ToString()
                                    + " Sample Rate: " + data.sample_rate.ToString()
                                    + " Samples to Get: " + data.samples_to_get.ToString()
                                    + " Range: " + data.range.ToString() + "g"
                                    + " AlarmX: " + data.alarm1.ToString() + "mm/s"
                                    + " AlarmY: " + data.alarm2.ToString() + "mm/s"
                                    + " AlarmZ: " + data.alarm3.ToString() + "mm/s"
                                    + "Alarm Check Interval " + data.alarmcheck_interval.ToString() + "s"
                                    + " (" + System.DateTime.Now.ToString() + ")"
                                    + "\r\n");


                                break;
                            }
                        case Phantom.Messages.RECEIVED_PHANTOM_ACCEL_STATE:
                            {

                                Phantom.MessageEIMONanoAccelState data = (Phantom.MessageEIMONanoAccelState)generic_data;

                                Console.WriteLine("State from " + data.phantom_code + " RMS Channel1: " + data.rms1.ToString()
                                    + " RMS Channel2: " + data.rms2.ToString()
                                    + " RMS Channel13: " + data.rms3.ToString()
                                    + " Battery voltage: " + data.battery.ToString() + "v"
                                    + " Temperature: " + data.temperature.ToString() + "C"
                                    + "\n");

                                break;
                            }
                        case Phantom.Messages.RECEIVED_PHANTOM_420_DATA:
                            {
                                Phantom.MessageData420 data = (Phantom.MessageData420)generic_data;

                                Console.WriteLine("4-20ma from " + data.phantom_code
                                    + " Voltage Channel 1: " + data.channel1_voltage.ToString()
                                    + " Voltage Channel 2: " + data.channel2_voltage.ToString()
                                    + " Voltage Channel 3: " + data.channel3_voltage.ToString()
                                    + " Voltage Channel 4: " + data.channel4_voltage.ToString()
                                    + " Battery voltage: " + data.battery.ToString() + "v"
                                    + " Temperature: " + data.module_temperature.ToString() + "C"
                                    + "\n");

                                break;
                            }
                    }
                }
                else
                {
                    System.Threading.Thread.Sleep(500);
                }
            }
        }
    }
}