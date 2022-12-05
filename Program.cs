using System.Runtime.ExceptionServices;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Diagnostics;
using System.Linq;
//using MISOTEN_APPLICATION.BackProcess;
namespace MISOTEN_APPLICATION.BackProcess.Mamoru
{
    static class Constants{
        //キャリブレーション許容誤差
        public const int second_joint_allowerror = 50;
        public const int third_joint_allowerror = 50;
        public const int pressure_allowerror = 50;
        public const int point_pressure_allowerror = 50;
        
        //スレーブ始動閾値
        public const float second_joint_threshold_pressure=0.0f;
        public const float third_joint_threshold_pressure=0.0f;
        public const float pressure_threshold_pressure=0.0f;
        public const float point_pressure_threshold_pressure=0.0f;

        //センサの最大値
        public const int sensor_max=1024;
    }

    // 角度格納用構造体
    public struct JOINT{
        public int second;
        public int third;
    }
    // 限界地格納用構造体
    public struct STATING_VALUE{
        public JOINT master;
        public JOINT SLAVE;
    }
    //可変抵抗、曲げセンサ、圧力センサの生データ
    public struct SENSOR_VALUE{
        public float second_joint;
        public float third_joint;
        public int pressure;
        public int point_pressure;
    }
    //出力値格納構造体
    public struct GODS_SENTENCE{
        public GOD_SENTENCE first_godsentence;
        public GOD_SENTENCE second_godsentence;
        public GOD_SENTENCE third_godsentence;
        public GOD_SENTENCE fourth_godsentence;
        public GOD_SENTENCE fifth_godsentence;
    }
    public struct GOD_SENTENCE{
        public int tip_pwm;
        public int palm_pwm;
    }
    // 第一、第二、第三関節間の長さ
    public struct LENGTH{
        public float first;
        public float second;
        public float third;
    }


    public class GodHand
    {
/*        static async Task Main(string[] args){
            {
                GodHand godhand=new GodHand();
                await godhand.run();
            }
        }
        */
        //動作している指
         //[0=小指][1=薬指][2=中指][3=人差し指][4=親指]
        public Boolean[] movement_finger = new Boolean[]{true,true,false,false,false};
        //GodFinger[5] gotfinger=new  GodFinger;
        public GodHand(){

        }
        //キャリブレーション
        public async Task<int> calibration(int _calibration_select)
        {  
            SENSOR_VALUE[] result_master=new SENSOR_VALUE[5];
            SENSOR_VALUE[] result_slave=new SENSOR_VALUE[5];
            if(_calibration_select==0){
                Task task1 = Task.Run(() => {
                    //マスター
                    result_master=calibration_inspection(true);
                    for(int index=0;index<5;index++){
                    //gotfinger[index].setStatingValue(result_master[index]);
                    Console.WriteLine("task1:"+index);
                    }
                });
                Task task2 = Task.Run(() => {
                    //スレーブ
                    result_slave=calibration_inspection(false);
                    for(int index=0;index<5;index++){
                    //gotfinger[index].setStatingValue(result_slave[index]);
                    Console.WriteLine("task2:"+index);
                    }
                });
                await Task.WhenAll(task1, task2);

            }else{
                //スレーブ
                result_slave=calibration_inspection(false);
                for(int index=0;index<5;index++){
                    Console.WriteLine("task1:"+index);
                //gotfinger.setEndingValue(result_slave);
                }
            }
            return 0;
            
        }
        public SENSOR_VALUE[] calibration_inspection(Boolean _calibration_select){
            System.Timers.Timer timer = new System.Timers.Timer(1000);
            Boolean[] finger_true = new Boolean[5];    
            //SENSOR_VALUE[0=小指][1=薬指][2=中指][3=人差し指][4=親指]
            SENSOR_VALUE[] Temporary_sensordate=new SENSOR_VALUE[5];
            List<SENSOR_VALUE>[] receive_log = new List<SENSOR_VALUE>[5];
            //通信クラスのインスタンス化
            //SignalClass signalClass=new SignalClass();
            //signalClass.ReciveData_Sensor recivedata_sensor =new signalClass.ReciveData_Sensor();
            Stopwatch sw = new Stopwatch();
            float truetime=0.0f;
            SENSOR_VALUE[] predate=new SENSOR_VALUE[5];
            SENSOR_VALUE[] avaragedate=new SENSOR_VALUE[5];
            timer.Elapsed += async (sender, e) =>
            {
                //通信クラスから構造体リストの5つ分の構造体を取り出す
                if(_calibration_select){
                    //getterで通信クラスからマスターの値を受け取る
                    //recivedata_sensor=GetMSensor();
                }else{
                    //getterで通信クラスからスレーブの値を受け取る
                    //recivedata_sensor=GetSSensor();
                }
                //Temporary_sensordate=recivedata_sensor.Little;
                //Temporary_sensordate=recivedata_sensor.Ring;
                //Temporary_sensordate=recivedata_sensor.Middle;
                //Temporary_sensordate=recivedata_sensor.Index;
                //Temporary_sensordate=recivedata_sensor.Thumb;
                //小指
                List<Task> arrayTask = new List<Task>();
                for(int count=0;count<5;count++){
                    int finger_count=count;
                    Task finger = Task.Run(  () => {
                        finger_true[finger_count]=finger_inspection(Temporary_sensordate[finger_count],predate[finger_count]);
                        //receive_log[finger_count].Add(Temporary_sensordate[finger_count]);
                        //predate[finger_count]=Temporary_sensordate[finger_count];
                    });
                    arrayTask.Add(finger);
                }
                await Task.WhenAll(arrayTask);
                arrayTask.Clear();
            };
            timer.Start();

            while(true){
                if(finger_true.All(i=>i==true)){
                        if(truetime==0.0f){
                            sw.Start();
                            truetime=sw.ElapsedMilliseconds;
                        }else{
                            truetime=sw.ElapsedMilliseconds;
                            if(truetime>3000){
                                timer.Stop();
                                break;
                            }
                        }
                }else{
                    sw.Restart();
                    Array.Clear(receive_log);
                }
            }
            
            for(int index=0;index<5;index++){
                receive_log[index].ForEach(iter => {
                    avaragedate[index].second_joint+=iter.second_joint;
                    avaragedate[index].third_joint+=iter.third_joint;
                    avaragedate[index].pressure+=iter.pressure;
                    avaragedate[index].point_pressure+=iter.point_pressure;
                });
                avaragedate[index].second_joint/=receive_log[index].Count;
                avaragedate[index].third_joint/=receive_log[index].Count;
                avaragedate[index].pressure/=receive_log[index].Count;
                avaragedate[index].point_pressure/=receive_log[index].Count;
            }
            
            return avaragedate;
        }

        public Boolean finger_inspection(SENSOR_VALUE _sensordate,SENSOR_VALUE _predate){
            Boolean[] finger_true = new Boolean[]{false,false,false,false};  
            if((Math.Abs(_sensordate.second_joint-_predate.second_joint))<Constants.second_joint_allowerror){
                finger_true[0]=true;
            }
            if((Math.Abs(_sensordate.third_joint-_predate.third_joint))<Constants.third_joint_allowerror){
                finger_true[1]=true;
            }
            if((Math.Abs(_sensordate.pressure-_predate.pressure))<Constants.pressure_allowerror){
                finger_true[2]=true;
            }
            if((Math.Abs(_sensordate.second_joint-_predate.point_pressure))<Constants.point_pressure_allowerror){
                finger_true[3]=true;
            }
            if(finger_true.All(i=>i==true)){
                return true;
            }else{
                return false;
            }
        }
        
        //圧力センサ始動監視
        public async Task<int> Threshold_monitoring(){
            SENSOR_VALUE[] Temporary_masterdate=new SENSOR_VALUE[5];
            SENSOR_VALUE[] Temporary_slavedate=new SENSOR_VALUE[5];
            //SignalClass signalClass=new SignalClass();
            //signalClass.ReciveData_Sensor recivedata_sensor =new signalClass.ReciveData_Sensor();
            while(true){
                //recivedata_sensor=GetMSensor();
                //Temporary_masterdate[0]=recivedata_sensor.Little;
                //Temporary_masterdate[1]=recivedata_sensor.Ring;
                //Temporary_masterdate[2]=recivedata_sensor.Middle;
                //Temporary_masterdate[3]=recivedata_sensor.Index;
                //Temporary_masterdate[4]=recivedata_sensor.Thumb;

                //recivedata_sensor=GetSSensor();
                //Temporary_slavedate[0]=recivedata_sensor.Little;
                //Temporary_slavedate[1]=recivedata_sensor.Ring;
                //Temporary_slavedate[2]=recivedata_sensor.Middle
                //Temporary_slavedate[3]=recivedata_sensor.Index;
                //Temporary_slavedate[4]=recivedata_sensor.Thumb;
                List<Task> arrayTask = new List<Task>();
                for(int count=0;count<5;count++){
                    int finger_count=count;
                    Console.WriteLine(finger_count);
                    if(movement_finger[finger_count]==false){
                        
                        Task finger = Task.Run(() => {
                            if(Temporary_masterdate[finger_count].second_joint==Constants.point_pressure_threshold_pressure){
                                if(finger_count!=4){
                                    movement_finger[finger_count]=finger_starting(Temporary_slavedate[finger_count],Temporary_slavedate[finger_count+1],finger_count);
                                }else{
                                    movement_finger[finger_count]=finger_starting(Temporary_slavedate[finger_count],Temporary_slavedate[finger_count-1],finger_count);
                                }
                            }
                        });
                        arrayTask.Add(finger);
                    }
                }
                await Task.WhenAll(arrayTask);
                arrayTask.Clear();
                //全指終了
                if(movement_finger.All(i=>i==true)){
                    Console.WriteLine("fingerall");
                     return 0;
                }
            }
        }
        public Boolean finger_starting(SENSOR_VALUE _sensordate,SENSOR_VALUE _nextsensordate ,int _finger_count){
            //float bandangle;
            //float resistangle;
            //float field_fingers;
            //float finger_height;
            //float nextfinger_height;
            //GodConverter godconverter=new GodConverter;
            //bandangle=godconverter.bendToAngle(_sensordate.pressure);
            //resistangle=godconverter.resistToAngle(_sensordate.point_pressure);
            //finger_height=forwardKinematics(Length,_sensordate.second_joint,_sensordate.third_joint);
            //nextfinger_height=forwardKinematics(Length[_finger_count],_nextsensordate.second_joint,_nextsensordate.third_joint);
            //field_fingers=godconverter.calcField(finger_height,nextfinger_height);
            //gotfinger[_finger_count].setField(field_fingers);
            //gotfinger[_finger_count].setHeight(finger_height);
            return true;
        }

        public async Task<int> run(){
            SENSOR_VALUE[] Temporary_masterdate=new SENSOR_VALUE[5];
            SENSOR_VALUE[] Temporary_slavedate=new SENSOR_VALUE[5];
            //recivedata_sensor=GetMSensor();
            //Temporary_masterdate[0]=recivedata_sensor.Little;
            //Temporary_masterdate[1]=recivedata_sensor.Ring;
            //Temporary_masterdate[2]=recivedata_sensor.Middle;
            //Temporary_masterdate[3]=recivedata_sensor.Index;
            //Temporary_masterdate[4]=recivedata_sensor.Thumb;

            //recivedata_sensor=GetSSensor();
            //Temporary_slavedate[0]=recivedata_sensor.Little;
            //Temporary_slavedate[1]=recivedata_sensor.Ring;
            //Temporary_slavedate[2]=recivedata_sensor.Middle
            //Temporary_slavedate[3]=recivedata_sensor.Index;
            //Temporary_slavedate[4]=recivedata_sensor.Thumb;
            GOD_SENTENCE[] finger_power =new GOD_SENTENCE[5];
            GODS_SENTENCE gods_senten=new GODS_SENTENCE();
            List<Task> arrayTask = new List<Task>();
            for(int count=0;count<5;count++){
                int finger_count =count;
                if(movement_finger[finger_count]==true){
                    Console.WriteLine(finger_count);
                    Task finger = Task.Run(() => {
                        if(finger_count!=4){
                            //gotfinger[finger_count].master_data=setSensorValue(Temporary_masterdate[finger_count]);
                            //gotfinger[finger_count].slave_data=setSensorValue(Temporary_masterdate[finger_count]);
                            //gotfinger[finger_count+1].slave_data=setSensorValue(Temporary_masterdate[finger_count]);
                            //finger_power[finger_count]=calc(gotfinger[finger_count],gotfinger[finger_count+1])
                        }else{
                            //gotfinger[finger_count].master_data=setSensorValue(Temporary_masterdate[finger_count]);
                            //gotfinger[finger_count].slave_data=setSensorValue(Temporary_masterdate[finger_count]);
                            //gotfinger[finger_count-1].slave_data=setSensorValue(Temporary_masterdate[finger_count]);
                            //finger_power[finger_count]=calc(gotfinger[finger_count],gotfinger[finger_count-1])
                        }
                    });
                    arrayTask.Add(finger);
                }
            }
            await Task.WhenAll(arrayTask);
            arrayTask.Clear();
            gods_senten.first_godsentence=finger_power[0];
            gods_senten.second_godsentence=finger_power[1];
            gods_senten.third_godsentence=finger_power[2];
            gods_senten.fourth_godsentence=finger_power[3];
            gods_senten.fifth_godsentence=finger_power[4];

            //スレーブに出力値を送信
            return 0;
        }
    }
}
