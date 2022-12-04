using System.Runtime.ExceptionServices;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Diagnostics;
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
        public const float second_joint_threshold_pressure=400.0f;
        public const float third_joint_threshold_pressure=400.0f;
        public const float pressure_threshold_pressure=400.0f;
        public const float point_pressure_threshold_pressure=400.0f;

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
    public struct GODS_SENTEMCE{
        //指先の出力値
        public float first_tip_pwm;
        public float second_tip_pwm;
        public float third_tip_pwm;
        public float fourth_tip_pwm;
        public float fifth_tip_pwm;
        //手の平の出力値
        public float first_palm_pwm;
        public float second_palm_pwm;
        public float third_palm_pwm;
        public float fourth_palm_pwm;
        public float fifth_palm_pwm;
    }
    // 第一、第二、第三関節間の長さ
    public struct LENGTH{
        public float first;
        public float second;
        public float third;
    }


    public class GodHand
    {
        //動作している指
         //[0=小指][1=薬指][2=中指][3=人差し指][4=親指]
        public Boolean[] movement_finger = new Boolean[]{false,false,false,false,false};
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
                    }
                });
                Task task2 = Task.Run(() => {
                    //スレーブ
                    result_slave=calibration_inspection(false);
                    for(int index=0;index<5;index++){
                    //gotfinger[index].setStatingValue(result_slave[index]);
                    }
                });
                await Task.WhenAll(task1, task2);

            }else{
                //スレーブ
                result_slave=calibration_inspection(false);
                for(int index=0;index<5;index++){
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
                for(int finger_count=0;finger_count<5;finger_count++){
                    Task finger = Task.Run(() => {
                        finger_true[finger_count]=finger_inspection(Temporary_sensordate[finger_count],predate[finger_count]);
                        receive_log[finger_count].Add(Temporary_sensordate[finger_count]);
                        predate[finger_count]=Temporary_sensordate[finger_count];
                    });
                }
                await Task.WhenAll(arrayTask);
                if(finger_true.All(i=>i==true)){
                    if(truetime==0.0f){
                        sw.Start();
                    }else{
                        truetime=sw.ElapsedMilliseconds;
                        if(truetime==3000){
                            timer.Stop();
                        }
                    }
                }else{
                    sw.Restart();
                    Array.Clear(receive_log);
                }
            };
            timer.Start();
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
            if((Math.Abs(_sensordate.second_joint-_predate.second_joint))==Constants.second_joint_allowerror){
                finger_true[0]=true;
            }
            if((Math.Abs(_sensordate.third_joint-_predate.third_joint))==Constants.third_joint_allowerror){
                finger_true[1]=true;
            }
            if((Math.Abs(_sensordate.pressure-_predate.pressure))==Constants.pressure_allowerror){
                finger_true[2]=true;
            }
            if((Math.Abs(_sensordate.second_joint-_predate.point_pressure))==Constants.point_pressure_allowerror){
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
                for(int finger_count=0;finger_count<5;finger_count++){
                    if(movement_finger[finger_count]==false){
                        Task finger = Task.Run(() => {
                            if(Temporary_masterdate[finger_count].second_joint>Constants.point_pressure_threshold_pressure){
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
                //全指終了
                if(movement_finger.All(i=>i==true)){
                    return 0;
                }
            }
            
        }
        public Boolean  finger_starting(SENSOR_VALUE _sensordate,SENSOR_VALUE _nextsensordate ,int _finger_count){
            float bandangle;
            float resistangle;
            float field_fingers;
            float finger_height;
            float nextfinger_height;
            //GodConverter godconverter=new GodConverter;
            //bandangle=godconverter.bendToAngle(_sensordate.pressure);
            //resistangle=godconverter.resistToAngle(_sensordate.point_pressure);
            //finger_height=forwardKinematics(Length,_sensordate.second_joint,_sensordate.third_joint);
            //nextfinger_height=forwardKinematics(Length[_finger_count],_nextsensordate.second_joint,_nextsensordate.third_joint);
            //field_fingers=godconverter.calcField(finger_height,nextfinger_height);
            //gotfinger[_finger_count].setField(field_fingers);

            return true;
        }

        public int calc(){
            SENSOR_VALUE[] Temporary_masterdate=new SENSOR_VALUE[5];
            SENSOR_VALUE[] Temporary_slavedate=new SENSOR_VALUE[5];
            LENGTH[] Length=new LENGTH[5];
            SENSOR_VALUE nextfinger;
            LENGTH nextlength=new LENGTH();
            float finger_height;
            float finger_density;
            float finger_field;
            float nextfinger_height;
            float max_power;
            float current_power;
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
            for(int finger_count=0;finger_count<5;finger_count++){
                finger_height=0.0f;
                finger_density=0.0f;
                finger_field=0.0f;
                
                if(finger_count!=4){
                    nextfinger=Temporary_masterdate[finger_count+1];
                    nextlength=Length[finger_count+1];
                }else{
                    nextfinger=Temporary_masterdate[finger_count-1];
                    nextlength=Length[finger_count-1];
                }
                if(movement_finger[finger_count]==true){
                    //ゴッドコンバータインスタンス化
                    //GodConverter godconverter=new GodConverter;
                    //マスター曲げセンサの値取得
                    //Temporary_masterdate[finger_count].second_joint=godconverter.bendToAngle(Temporary_masterdate[finger_count].second_joint);
                    //マスター可変抵抗の値取得
                    //Temporary_masterdate[finger_count].third_joint=godconverter.resistToAngle(Temporary_masterdate[finger_count].third_joint);
                    //ゴッドフィンガーにセンサーデータを格納
                    //gotfinger[finger_count].setSensorValue(Temporary_masterdate[finger_count]);
                    //関節間の指の長さ取得
                    //Length[finger_count]=getLength(finger_count);
                    
                    //比率計算用最大値計算
                    //指の高さを順運動学で取得
                    //finger_height=godconverter.forwardKinematics(Length[finger_count],Temporary_masterdate[finger_count].second_joint,Temporary_masterdate[finger_count].third_joint);
                    //nextfinger_height=godconverter.forwardKinematics(nextlength[finger_count],nextfinger.second_joint,nextfinger.third_joint);                
                    //モノの密度計算
                    //finger_density=godconverter.calcDensity(finger_height,sensor_max,sensor_max);
                    //マスター現在の面積計算
                    //finger_field=godconverter.calcField(finger_height,nextfinger_height);
                    //max_power=finger_density*ratiio(gotfinger[finger_count],finger_field); 

                    //比率計算用現在値計算
                    //指の高さを順運動学で取得
                    //finger_height=godconverter.forwardKinematics(Length[finger_count],Temporary_masterdate[finger_count].second_joint,Temporary_masterdate[finger_count].third_joint);
                    //nextfinger_height=godconverter.forwardKinematics(nextlength[finger_count],nextfinger.second_joint,nextfinger.third_joint);                
                    //モノの密度計算
                    //finger_density=godconverter.calcDensity(finger_height,Temporary_masterdate[finger_count].pressure,Temporary_masterdate[finger_count].point_pressure);
                    //マスター現在の面積計算
                    //finger_field=godconverter.calcField(finger_height,nextfinger_height);
                    //current_power=finger_density*ratiio(gotfinger[finger_count],finger_field);         

                    //pwmの送信           
                }
            }
            return 0;
        }
    }
}
