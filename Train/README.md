# RNN 帧颜色预测训练项目

这个项目使用循环神经网络(RNN)来预测视频序列中的下一帧颜色。输入包括上一帧的RGBA颜色、UV坐标和时间差，输出为下一帧的RGBA颜色。

## 目录结构
```
Train/
├── nn_train.py       # 主训练脚本
├── prepare_data.py   # 数据准备脚本
├── run.bat           # Windows批处理运行脚本
├── utils/            # 工具函数目录
├── data/             # 训练数据目录
└── models/           # 模型保存目录
```

## 环境要求
- Python 3.7+
- PyTorch 1.7+
- NumPy
- Matplotlib
- scikit-learn

## 使用方法

### 1. 准备数据
首先生成训练数据：
```bash
python prepare_data.py --num_sequences 1000 --min_length 5 --max_length 10 --noise_level 0.05 --save_dir ./data
```
参数说明：
- `--num_sequences`: 生成的序列数量
- `--min_length`: 序列的最小长度
- `--max_length`: 序列的最大长度
- `--noise_level`: 颜色变化的噪声水平
- `--save_dir`: 数据保存目录

### 2. 训练模型
可以通过以下命令训练模型：
```bash
python nn_train.py --data_dir ./data --epochs 100 --batch_size 16 --hidden_size 128 --num_layers 2 --rnn_type LSTM --lr 0.001 --save_dir ./models
```
或者在Windows上使用批处理脚本：
```bash
run.bat
```
参数说明：
- `--data_dir`: 数据目录
- `--epochs`: 训练轮数
- `--batch_size`: 批次大小
- `--hidden_size`: RNN隐藏层大小
- `--num_layers`: RNN层数
- `--rnn_type`: RNN类型 (LSTM, GRU, RNN)
- `--lr`: 学习率
- `--save_dir`: 模型保存目录

### 3. 使用训练好的模型
训练好的模型会保存在`./models`目录下，可以通过加载模型来预测下一帧颜色。示例代码：
```python
import torch
from nn_train import ColorPredictorRNN, predict_next_frame

# 加载模型
model = ColorPredictorRNN(input_size=7, hidden_size=128, num_layers=2, rnn_type='LSTM')
model.load_state_dict(torch.load('models/best_model_LSTM_h128_l2.pth'))
model.eval()

# 预测下一帧颜色
last_frame_features = [0.5, 0.5, 0.5, 1.0, 0.5, 0.5, 0.1]  # [r, g, b, a, u, v, dt]
predicted_color, hidden = predict_next_frame(model, last_frame_features)
print(f'预测的下一帧颜色: {predicted_color}')
```

## 模型结构
- 输入层: 7个特征 (RGBA + UV + dt)
- RNN层: 可配置的层数和隐藏层大小，支持LSTM、GRU和普通RNN
- 输出层: 4个特征 (下一帧的RGBA)

## 注意事项
1. 确保数据目录`./data`存在，并且包含有效的训练数据
2. 训练过程中会自动保存最佳模型到`./models`目录
3. 训练完成后会生成损失曲线图像`loss_curve.png`
4. 可以通过调整超参数来优化模型性能

## 扩展建议
1. 添加更多的特征，如前几帧的颜色信息
2. 尝试不同的网络结构，如双向RNN或CNN-RNN混合模型
3. 使用真实的视频数据进行训练
4. 实现更复杂的损失函数，如感知损失

希望这个项目对你有所帮助！如果有任何问题或建议，请随时提出。