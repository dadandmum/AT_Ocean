@echo off

:: 设置Python环境（如果需要）
:: 例如: call activate your_env

:: 切换到脚本目录
cd /d %~dp0

:: 运行训练脚本
python nn_train.py

:: 暂停以便查看输出
pause