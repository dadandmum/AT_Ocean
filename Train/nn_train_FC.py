import torch
import torch.nn as nn
import torch.optim as optim
from torch.utils.data import Dataset, DataLoader
import numpy as np
import os
import matplotlib.pyplot as plt
from sklearn.model_selection import train_test_split
import json
import pathlib
import re

# --------------------------------------
# 1. 数据加载与预处理
# --------------------------------------
# def load_data(data_dir, test_split=0.2):
#     """
#     加载数据并分割为训练集和测试集
#     data_dir: 数据目录
#     test_split: 测试集比例
#     """
#     sequences = []
#     # 遍历数据目录中的所有文件
#     for filename in os.listdir(data_dir):
#         if filename.endswith('.npy'):
#             filepath = os.path.join(data_dir, filename)
#             seq = np.load(filepath)
#             sequences.append(seq)

#     # 分割训练集和测试集
#     train_sequences, test_sequences = train_test_split(sequences, test_size=test_split, random_state=42)
#     return train_sequences, test_sequences

def load_data(data_dir, deltaT, input_stack_num, test_split=0.2):

    import OpenEXR  # 用于读取 EXR 文件
    import Imath    # 用于处理 EXR 文件的数据类型
    import array    # 用于处理像素数据
    # 存储文件名和对应的序号
    exr_files = {}
    # 遍历数据目录中的所有文件
    for filename in os.listdir(data_dir):
        if filename.endswith('.exr'):
            match = re.search(r'^(.+?)_\d+\.exr$', filename)
            if match:
                name = match.group(1)
                if name not in exr_files:
                    exr_files[name] = []
                exr_files[name].append(filename)

    # 打印读取文件名
    for name, files in exr_files.items():
        print(f"Image Name: {name} , Files Count: {len(files)}")
        
        
    # 遍历每个 tag，根据不同的 tag 读取数据
    sequences = {}
    for tag in sorted(exr_files.keys()):
        # 对当前 tag 下的文件按序号排序
        sorted_files = sorted(exr_files[tag])
        
        rgbauv_sequence = []
        
        for filename in sorted_files:
            filepath = os.path.join(data_dir, filename)
            try:
                # 打开 EXR 文件
                exr_file = OpenEXR.InputFile(filepath)
                # 获取图像的头信息
                header = exr_file.header()
                dw = header['dataWindow']
                # 获取图像尺寸
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

                # 将像素数据组合为 RGBA+UV 格式
                # 每个元素的格式为 [r, g, b, a, u, v]
                # 总维度为 [width * height, 6]
                rgba_uv = []
                for i in range(len(r)):
                    # 计算 uv 坐标 (归一化到 [0, 1])
                    x = i % width
                    y = i // width
                    u = 1.0 * x / (width - 1) if width > 1 else 0
                    v = 1.0 * y / (height - 1) if height > 1 else 0
                    rgba_uv.append([r[i], g[i], b[i], a[i], u, v])

                # 将当前图像的像素数据添加到当前 tag 的序列中
                # 总维度为 [image_count , width * height, 6]

                rgbauv_sequence.append(np.array(rgba_uv,dtype=np.float32))

        
            except Exception as e:
                print(f"Error when load file {filepath} : {e}")
        
        rgba_uv_dt_target_seq = []
        # 增加时间差
        # 对每一个索引
        for stack_num in range(input_stack_num):
            # 对每一个图片(时间轴上的一点)
            for k in range(len(rgbauv_sequence)-stack_num):
                t = k + stack_num
                dt = (t - k) * deltaT
                rgba_uv_dt_target = []
                for i in range(len(rgbauv_sequence[k])):
                    rgba_uv_dt_target.append( np.array(list(rgbauv_sequence[k][i]) + [dt] + list(rgbauv_sequence[t][i][:4]),dtype=np.float32) )

                rgba_uv_dt_target_seq.append(np.array(rgba_uv_dt_target,dtype=np.float32))


        
        if rgba_uv_dt_target_seq:
            sequences[tag] = rgba_uv_dt_target_seq
            print(f"Load Data from TAG [ {tag} ] , Total Seq: {len(rgba_uv_dt_target_seq)}")

    # 根据tag 分割对应的数据队列
    train_test_sequences = {}
    for tag, tag_sequence in sequences.items():
        train_seq, test_seq = train_test_split(tag_sequence, test_size=test_split, random_state=42)
        train_test_sequences[tag] = (train_seq , test_seq)
    return train_test_sequences

# --------------------------------------
# 2. 自定义 Dataset
# --------------------------------------
class FramePredictionDataset(Dataset):
    def __init__(self, sequences):
        # sequences: list of numpy arrays, each of shape [T, 7+4=11]
        # 每个时间步: [r, g, b, a, u, v, dt, target_r, target_g, target_b, target_a]
        self.sequences = sequences

    def __len__(self):
        return len(self.sequences)

    def __getitem__(self, idx):
        seq = self.sequences[idx]  # shape: [T, 11]
        inputs = seq[:, :7]      # 输入: 从第0到倒数第二帧的 (RGBA+UV+dt)
        targets = seq[:, 7:11]    # 输出: 从第1到最后一帧的 RGBA (下一帧颜色)

        return torch.FloatTensor(inputs), torch.FloatTensor(targets)


def collate_fn(batch):
    """
    自定义数据加载器的collate函数，用于处理变长序列
    """
    inputs, targets = zip(*batch)
    # 找出最长序列长度
    max_len = max([seq.shape[0] for seq in inputs])
    batch_size = len(inputs)
    input_size = inputs[0].shape[1]
    target_size = targets[0].shape[1]

    # 创建填充后的张量
    padded_inputs = torch.zeros(batch_size, max_len, input_size)
    padded_targets = torch.zeros(batch_size, max_len, target_size)

    # 填充数据
    for i, (input_seq, target_seq) in enumerate(zip(inputs, targets)):
        seq_len = input_seq.shape[0]
        padded_inputs[i, :seq_len, :] = input_seq
        padded_targets[i, :seq_len, :] = target_seq

    return padded_inputs, padded_targets


# --------------------------------------
# 新增：全连接模型定义
# --------------------------------------
class ColorPredictorFC(nn.Module):
    def __init__(self, input_size=7, hidden_size=[128, 256, 128], output_size=4):
        """
        全连接神经网络模型，用于预测颜色
        :param input_size: 输入特征维度，默认为 7
        :param hidden_size: 隐藏层维度列表，默认为 [128, 256, 128]
        :param output_size: 输出特征维度，默认为 4 (RGBA)
        """
        super(ColorPredictorFC, self).__init__()
        layers = []
        prev_size = input_size
        
        # 构建隐藏层
        for size in hidden_size:
            layers.append(nn.Linear(prev_size, size))
            layers.append(nn.ReLU())
            prev_size = size
        
        # 构建输出层
        layers.append(nn.Linear(prev_size, output_size))
        
        self.fc_layers = nn.Sequential(*layers)

    def forward(self, x):
        # 对于输入形状为 [batch, seq_len, input_size] 的数据，先展平处理
        batch_size, seq_len = x.size(0), x.size(1)
        x = x.view(batch_size * seq_len, -1)  # 展平序列
        
        out = self.fc_layers(x)
        out = out.view(batch_size, seq_len, -1)  # 恢复序列形状
        return out



# --------------------------------------
# 4. 模型评估
# --------------------------------------
def evaluate(model, dataloader, criterion, device):
    model.eval()
    total_loss = 0
    with torch.no_grad():
        for inputs, targets in dataloader:
            inputs, targets = inputs.to(device), targets.to(device)
            outputs = model(inputs)
            loss = criterion(outputs, targets)
            total_loss += loss.item()
    return total_loss / len(dataloader)


# --------------------------------------
# 5. 训练流程
# --------------------------------------
def train(data_dir='./data', deltaT = 0.016, input_stack_num = 3, epochs=100, batch_size=16, hidden_size=128, num_layers=2, lr=1e-3, save_dir='./models', output_batch_size=4096):

    # 确保保存目录存在
    os.makedirs(save_dir, exist_ok=True)
    
    # 加载数据
    train_test_sequences = load_data(data_dir, deltaT, input_stack_num)

    # 输出数据集信息
    print(" ======== Data Info ============")
    for tag, (train_seq, test_seq) in train_test_sequences.items():
        print(f"   Tag: {tag}, Train Seq: {len(train_seq)}, Test Seq: {len(test_seq)}")
    print(" ======== Data Info ============")
    # 遍历每个标签进行训练
    for tag, (train_seq, test_seq) in train_test_sequences.items():
        print(f" ======== Training on tag {tag} ============")
        train_by_tag(tag, train_seq, test_seq, save_dir, epochs , batch_size , hidden_size , num_layers  , lr, output_batch_size)


    
    
def train_by_tag(tag, train_seq, test_seq, save_dir, epochs=100, batch_size=32 , hidden_size=128, num_layers=2, lr=1e-3, output_batch_size=4096):

    
    # 创建数据集和数据加载器
    train_dataset = FramePredictionDataset(train_seq)
    test_dataset = FramePredictionDataset(test_seq)
    train_dataloader = DataLoader(train_dataset, batch_size=batch_size, shuffle=True, collate_fn=collate_fn)
    test_dataloader = DataLoader(test_dataset, batch_size=batch_size, shuffle=False, collate_fn=collate_fn)

    # 模型、损失函数、优化器
    device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')
    model = ColorPredictorFC(input_size=7, hidden_size=hidden_size,output_size=4).to(device)

    criterion = nn.MSELoss()
    optimizer = optim.Adam(model.parameters(), lr=lr)
    scheduler = optim.lr_scheduler.ReduceLROnPlateau(optimizer, mode='min', factor=0.5, patience=5, verbose=True)

    # 记录损失
    train_losses = []
    test_losses = []

    # 训练循环
    best_test_loss = float('inf')
    last_model_path = None
    for epoch in range(epochs+1):
        model.train()
        total_loss = 0
        for inputs, targets in train_dataloader:
            inputs, targets = inputs.to(device), targets.to(device)

            optimizer.zero_grad()
            outputs = model(inputs)
            loss = criterion(outputs, targets)
            loss.backward()
            optimizer.step()

            total_loss += loss.item()

        # 计算平均损失
        train_loss = total_loss / len(train_dataloader)
        test_loss = evaluate(model, test_dataloader, criterion, device)

        # 记录损失
        train_losses.append(train_loss)
        test_losses.append(test_loss)

        # 更新学习率
        scheduler.step(test_loss)

        # 打印信息
        print(f' >{tag} ~ Epoch [{epoch+1}/{epochs}], Train Loss: {train_loss:.6f}, Test Loss: {test_loss:.6f}')

        # 保存模型
        if epoch % 20 == 0:
            model_path = os.path.join(save_dir, f'{tag}_FC_l{num_layers}_{epoch}.pth')
            torch.save(model.state_dict(), model_path)
            last_model_path = model_path
            print(f'==== Save model to: {model_path} ====')

    # 绘制损失曲线
    plt.figure(figsize=(10, 6))
    plt.plot(train_losses, label='Train Loss')
    plt.plot(test_losses, label='Test Loss')
    plt.xlabel('Epoch')
    plt.ylabel('Loss')
    plt.legend()
    plt.savefig(os.path.join(save_dir, f'{tag}_FC_loss_curve.png'))

    plt.close()

    # 保存模型为 ONNX
    modelONNX = ColorPredictorFC(input_size=7, hidden_size=hidden_size, output_size=4)
    modelONNX.load_state_dict(torch.load(last_model_path))
    onnx_save_path = os.path.join(save_dir, f'{tag}_FC_model.onnx')
    save_model_as_onnx(modelONNX, onnx_save_path, output_batch_size=output_batch_size)


    return model

def save_model_as_onnx(model, save_path, input_size=(1, 1, 7) , output_batch_size = 4096):
    """
    将模型保存为 ONNX 格式
    :param model: 训练好的模型
    :param save_path: ONNX 文件保存路径
    :param input_size: 输入张量的形状，默认为 [1, 1, 7]
    """
    model.eval()
    dummy_input = torch.randn(output_batch_size, 1, 1, 7)

    
    
    torch.onnx.export(
        model,
        dummy_input,
        save_path,
        opset_version=11,
        export_params=True,
        output_names=['output'],
        input_names=['input'],
        # dynamic_axes={
        #     'input': {0: 'batch_size', },
        #     'output': {0: 'batch_size', }
        # }
    )
    
    # torch.onnx.export(
    #     model,
    #     dummy_input,
    #     save_path,
    #     opset_version=11,
    #     export_params=True,
    #     output_names=['output'],
    #     input_names=['input'],
    # )
    
    # # 将模型保存为 ONNX 格式
    # torch.onnx.export(
    #     model,
    #     dummy_input,
    #     save_path,
    #     export_params=True,
    #     opset_version=11,
    #     do_constant_folding=True,
    #     input_names=['input'],
    #     output_names=['output'],
    #     dynamic_axes={
    #         'input': {0: 'batch_size', 1: 'seq_len'},
    #         'output': {0: 'batch_size', 1: 'seq_len'}
    #     }
    # )
    print(f'Model saved as ONNX format to: {save_path}')



# --------------------------------------
# 6. 预测函数
# --------------------------------------
def predict_next_frame(model, last_frame_features, hidden=None):
    """
    预测下一帧颜色
    model: 训练好的模型
    last_frame_features: 上一帧的特征 [r, g, b, a, u, v, dt]
    hidden: 上一时刻的隐藏状态
    """
    model.eval()
    with torch.no_grad():
        # 调整输入形状: [1, 1, 7]
        input_tensor = torch.FloatTensor(last_frame_features).unsqueeze(0).unsqueeze(0)
        output, new_hidden = model(input_tensor, hidden)
        # 提取预测结果: [1, 1, 4] -> [4]
        predicted_color = output.squeeze(0).squeeze(0).numpy()
    return predicted_color, new_hidden


# --------------------------------------
# 7. 主函数
# --------------------------------------
if __name__ == '__main__':
    # 从配置文件中读取参数
    config_path = 'config_FC.json'
    try: 
        with open(config_path, 'r') as f:
            config = json.load(f)
    except FileNotFoundError:
        # 获取当前脚本所在目录
        script_dir = pathlib.Path(__file__).parent
        config_path = script_dir / config_path
        with open(config_path, 'r') as f:
            config = json.load(f)
    
    # 打印配置信息
    print(f'Loading parameters from config file {config_path}:')
    for key, value in config.items():
        print(f'  {key}: {value}')

    # 开始训练
    # print(f'Start training the {config["rnn_type"]} model...')
    export_onnx_only = False
    
    if (export_onnx_only):
        
        tags = ["Gerstner_Displacement","Gerstner_Normal"]
        
        for tag in tags:
            model = ColorPredictorFC(input_size=7, hidden_size=config['hidden_size'], output_size=4)
            model_path = os.path.join(config['save_dir'], f'{tag}_FC_l{len(config["hidden_size"])}_{config["epochs"]}.pth')
            model.load_state_dict(torch.load(model_path))
            onnx_path = os.path.join(config['save_dir'], f'{tag}_FC_model.onnx')
            save_model_as_onnx(model, onnx_path, output_batch_size=config['output_batch_size'])

            
        exit()
    
    
    model = train(
        data_dir=config['data_dir'],
        deltaT=config['deltaT'],
        input_stack_num=config['input_stack_num'],
        epochs=config['epochs'],
        batch_size=config['batch_size'],
        hidden_size=config['hidden_size'],
        num_layers=len(config['hidden_size']),
        lr=config['lr'],
        save_dir=config['save_dir'],
        output_batch_size=config['output_batch_size']

    )


    print('Training completed!')
