Проект предназначен для моделирования алгоритма VinKekFish с длиной ключа 4096 битов.

# VinKekFish
Описание алгоритма VinKekFish
VinKekFish\main_tests\Задачи и другое\Криптография\Размышления\VinKekFish.md

## Тесты на разработку алгоритма

VinKekFish_cryptoDev\permutationsTest\old\VinKekFish\PermutationDiffusionTest.cs
	Этот тест имитирует диффузию между блоками. Диффузия осуществляется примерно за 6 раундов (keccak+перестановка+threefish+перестановка). Это 3 раунда VinKekFish

VinKekFish_cryptoDev\permutationsTest\old\VinKekFish\TweakTest.cs
	Тест просто подсчитывает количества возможных вариантов счётчика при определённых режимах приращений

## Тесты на проверку алгоритма

VinKekFish_cryptoDev\permutationsTest\GammaOverwrite_VinKekFishTest.cs
	Тест должен вычислить длину гаммы в режиме Overwrite с вырожденным внутренним состоянием (слишком маленьким)

