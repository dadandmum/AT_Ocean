import torch
from ultralytics import YOLO


def export_yolo_to_onnx(yolo_model_path, onnx_output_path):
    """
    加载YOLO模型并将其导出为ONNX格式
    
    参数:
    yolo_model_path (str): YOLO模型文件的路径
    onnx_output_path (str): 导出的ONNX文件的保存路径
    """
    try:
        # 加载YOLO模型
        # model = torch.hub.load('ultralytics/yolov5', 'custom', path=yolo_model_path)
        model = YOLO(yolo_model_path)
        
        print("load model success!")

        # 导出模型为ONNX格式
        model.export(format='onnx')

        print(f"模型已成功导出到 {onnx_output_path}")
                
        # 加载导出的ONNX模型
        onnx_model = YOLO(onnx_output_path)
        # run inference
        results = onnx_model("https://ultralytics.com/images/bus.jpg")
        
        # 输出结果
        print("result is ", results)

        
        


    except Exception as e:
        print(f"导出模型时出错: {e}")

if __name__ == "__main__":
    # 指定YOLO模型路径和导出的ONNX文件路径
    yolo_model_path = 'Train/models/yolo11n-pose.pt'
    onnx_output_path = 'Train/models/yolo11n-pose.onnx'
    
    # 调用导出函数
    export_yolo_to_onnx(yolo_model_path, onnx_output_path)
