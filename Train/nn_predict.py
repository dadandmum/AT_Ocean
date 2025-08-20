
import onnxruntime as ort
import OpenEXR
import Imath
import array
import os
def predict_with_onnx(onnx_model_path, input_exr_path, dt, output_exr_path):
    """
    使用 ONNX 模型对输入的 EXR 贴图进行预测
    :param onnx_model_path: ONNX 模型路径
    :param input_exr_path: 输入的 EXR 贴图路径
    :param dt: 预测时间
    :param output_exr_path: 输出的预测结果 EXR 贴图路径
    """
    # 初始化 ONNX 运行时
    session = ort.InferenceSession(onnx_model_path)
    
    # 打开输入的 EXR 文件
    exr_file = OpenEXR.InputFile(input_exr_path)
    header = exr_file.header()
    dw = header['dataWindow']
    width = dw.max.x - dw.min.x + 1
    height = dw.max.y - dw.min.y + 1

    # 读取 RGBA 通道数据
    pt = Imath.PixelType(Imath.PixelType.FLOAT)
    r_str = exr_file.channel('R', pt)
    g_str = exr_file.channel('G', pt)
    b_str = exr_file.channel('B', pt)
    a_str = exr_file.channel('A', pt)

    # 将字节数据转换为浮点数数组
    r = array.array('f', r_str)
    g = array.array('f', g_str)
    b = array.array('f', b_str)
    a = array.array('f', a_str)

    # 准备预测结果
    predicted_r = []
    predicted_g = []
    predicted_b = []
    predicted_a = []

    # 准备所有像素的输入特征
    input_features = []
    for i in range(len(r)):
        # 计算 uv 坐标 (归一化到 [0, 1])
        x = i % width
        y = i // width
        u = 1.0 * x / (width - 1) if width > 1 else 0
        v = 1.0 * y / (height - 1) if height > 1 else 0

        # 准备输入特征 [r, g, b, a, u, v, dt]
        input_features.append([r[i], g[i], b[i], a[i], u, v, dt])

    # 调整输入形状为 [1, num_pixels, 7]
    input_tensor = [input_features]

    # 进行预测
    outputs = session.run(None, {'input': input_tensor})
    # 提取预测结果 [1, num_pixels, 4] -> [num_pixels, 4]
    predicted_colors = outputs[0][0]
    
    # 将预测结果拆分到各个通道
    for color in predicted_colors:
        predicted_r.append(color[0])
        predicted_g.append(color[1])
        predicted_b.append(color[2])
        predicted_a.append(color[3])

    # 将预测结果转换为字节数据
    r_str_out = array.array('f', predicted_r).tobytes()
    g_str_out = array.array('f', predicted_g).tobytes()
    b_str_out = array.array('f', predicted_b).tobytes()
    a_str_out = array.array('f', predicted_a).tobytes()

    # 创建新的 EXR 文件并写入预测结果
    header = OpenEXR.Header(width, height)
    header['channels'] = {
        'R': Imath.Channel(Imath.PixelType(Imath.PixelType.FLOAT)),
        'G': Imath.Channel(Imath.PixelType(Imath.PixelType.FLOAT)),
        'B': Imath.Channel(Imath.PixelType(Imath.PixelType.FLOAT)),
        'A': Imath.Channel(Imath.PixelType(Imath.PixelType.FLOAT))
    }
    
    # 如果没有输出路径，创建一个新的文件夹
    
    if not os.path.exists(os.path.dirname(output_exr_path)):
        os.makedirs(os.path.dirname(output_exr_path))
    
    out_exr = OpenEXR.OutputFile(output_exr_path, header)
    out_exr.writePixels({
        'R': r_str_out,
        'G': g_str_out,
        'B': b_str_out,
        'A': a_str_out
    })
    print(f'Prediction result saved to {output_exr_path}')
    
    
if __name__ == "__main__":
    # 示例参数，可根据实际情况修改
    onnx_model_path = "Train/models/Gerstner_Displacement_RNN_model.onnx"
    input_exr_path = "Assets/ATOcean/Output/Gerstner_128_100/Gerstner_Displacement_00050.exr"
    dt = 0.033
    output_exr_path = "Train/Output/Gerstner_Displacement_00050_pred.exr"

    
    # 调用预测函数
    predict_with_onnx(onnx_model_path, input_exr_path, dt, output_exr_path)

