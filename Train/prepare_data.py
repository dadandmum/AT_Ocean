import numpy as np
import os
import argparse

"""
数据准备脚本：生成用于训练RNN的帧序列数据
输入格式：每一帧的特征包括 [r, g, b, a, u, v, dt]
输出格式：每一帧的目标颜色为 [target_r, target_g, target_b, target_a]
"""

def generate_sample_sequence(length, noise_level=0.05):
    """
    生成样本序列
    length: 序列长度
    noise_level: 噪声水平
    """
    seq = np.zeros((length, 11), dtype=np.float32)  # [r, g, b, a, u, v, dt, target_r, target_g, target_b, target_a]

    # 初始帧颜色
    r, g, b, a = np.random.uniform(0, 1, 4)

    # 生成序列
    for i in range(length):
        # 生成UV坐标 (简单的空间位置)
        u = np.sin(i * 0.1) * 0.5 + 0.5
        v = np.cos(i * 0.1) * 0.5 + 0.5

        # 时间差 (假设每帧时间间隔相同)
        dt = 0.1

        # 当前帧特征
        seq[i, 0] = r  # r
        seq[i, 1] = g  # g
        seq[i, 2] = b  # b
        seq[i, 3] = a  # a
        seq[i, 4] = u  # u
        seq[i, 5] = v  # v
        seq[i, 6] = dt  # dt

        # 添加噪声
        r += np.random.uniform(-noise_level, noise_level)
        g += np.random.uniform(-noise_level, noise_level)
        b += np.random.uniform(-noise_level, noise_level)
        a += np.random.uniform(-noise_level, noise_level)

        # 确保颜色值在0-1之间
        r = np.clip(r, 0, 1)
        g = np.clip(g, 0, 1)
        b = np.clip(b, 0, 1)
        a = np.clip(a, 0, 1)

        # 下一帧颜色作为目标
        if i < length - 1:
            seq[i, 7] = r
            seq[i, 8] = g
            seq[i, 9] = b
            seq[i, 10] = a

    return seq

def prepare_dataset(num_sequences, min_length=5, max_length=10, noise_level=0.05, save_dir='./data'):
    """
    准备数据集
    num_sequences: 序列数量
    min_length: 最小序列长度
    max_length: 最大序列长度
    noise_level: 噪声水平
    save_dir: 保存目录
    """
    # 确保保存目录存在
    os.makedirs(save_dir, exist_ok=True)

    # 生成序列
    for i in range(num_sequences):
        # 随机序列长度
        length = np.random.randint(min_length, max_length + 1)
        # 生成序列
        seq = generate_sample_sequence(length, noise_level)
        # 保存序列
        save_path = os.path.join(save_dir, f'sequence_{i}.npy')
        np.save(save_path, seq)
        print(f'保存序列 {i+1}/{num_sequences} 到 {save_path}')

    print(f'数据集准备完成，共 {num_sequences} 条序列，保存在 {save_dir}')


if __name__ == '__main__':
    # 解析命令行参数
    parser = argparse.ArgumentParser(description='准备RNN训练数据')
    parser.add_argument('--num_sequences', type=int, default=1000, help='序列数量')
    parser.add_argument('--min_length', type=int, default=5, help='最小序列长度')
    parser.add_argument('--max_length', type=int, default=10, help='最大序列长度')
    parser.add_argument('--noise_level', type=float, default=0.05, help='噪声水平')
    parser.add_argument('--save_dir', type=str, default='./data', help='数据保存目录')
    args = parser.parse_args()

    # 准备数据集
    prepare_dataset(args.num_sequences, args.min_length, args.max_length, args.noise_level, args.save_dir)