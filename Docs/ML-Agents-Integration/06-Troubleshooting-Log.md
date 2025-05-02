# 06. Python 환경 설정 문제 해결 기록

ML-Agents 학습 환경 (Python) 설정 중 발생했던 문제들과 해결 과정을 기록한다.

## 초기 설정
- **Unity:** ML-Agents 패키지 v3.0.0 (`com.unity.ml-agents`) 설치
- **Python:** `mlagents` v0.28.0, `torch` v2.7.0 설치 시도 (초기 시스템 Python 3.13 환경)

## 문제 1: Protobuf 버전 충돌

- **증상:** `mlagents-learn` 실행 시 `TypeError: Descriptors cannot be created directly... regenerated with protoc >= 3.19.0` 에러 발생.
- **원인:** 시스템에 설치된 `protobuf` 버전 (아마 4.x 또는 5.x)이 `mlagents` 0.28.0이 요구하는 버전 (3.20.x 이하)과 호환되지 않음.
- **해결:** `protobuf` 버전을 3.20.0으로 다운그레이드.
  ```bash
  pip install protobuf==3.20.0
  ```

## 문제 2: `cattr` 관련 TypeError (Python 3.13)

- **증상:** `protobuf` 문제 해결 후 `mlagents-learn` 실행 시 `TypeError: Invalid first argument to register(). typing.Dict[...] is not a class or union type.` 에러 발생.
- **원인 추정:** `mlagents` 0.28.0 버전이 Python 3.13 버전과 호환되지 않음. (특히 `cattr` 라이브러리와의 상호작용 문제로 추정)
- **시도 1:** Python 3.9 가상 환경 생성 및 패키지 재설치.
  - `py -3.9 -m venv .venv-mlagents`
  - `.venv-mlagents\Scripts\Activate.ps1`
  - `pip install mlagents==0.28.0 torch==2.7.0 protobuf==3.20.0`
- **결과 1:** 동일한 `TypeError` 발생.

## 문제 3: 가상 환경 패키지 인식 오류

- **증상:** Python 3.9 가상 환경 활성화 후 `python -m mlagents.trainers.learn ...` 실행 시 `ModuleNotFoundError: No module named 'mlagents'` 발생. `pip show mlagents` 확인 결과, 패키지가 가상 환경이 아닌 시스템 전역 Python 3.13 경로에 설치되어 있었음.
- **원인:** 초기 `pip install` 시 가상 환경 활성화가 불완전했거나 다른 요인으로 인해 전역 경로에 설치됨.
- **시도 2:** 가상 환경 내부에 패키지 강제 재설치 (`--target` 사용).
  ```bash
  pip install --target=\".venv-mlagents\\Lib\\site-packages\" --force-reinstall mlagents==0.28.0
  ```
- **결과 2:** 여전히 `TypeError` 발생 (모듈은 찾았으나 근본적인 호환성 문제 해결 안 됨).

## 문제 4: NumPy Import Error

- **증상:** 강제 재설치 이후 `python -m mlagents.trainers.learn ...` 실행 시 `ImportError: Error importing numpy... Original error was: No module named 'numpy.core._multiarray_umath'` 발생.
- **원인:** `--target` 옵션을 사용한 강제 설치 과정에서 NumPy 설치가 손상되었을 가능성 높음.
- **시도 3:** 가상 환경 완전 삭제 후 재생성 및 표준 방식으로 패키지 재설치.
  - `deactivate` (PowerShell에서는 효과 없음)
  - `Remove-Item -Recurse -Force .venv-mlagents`
  - `py -3.9 -m venv .venv-mlagents`
  - `.venv-mlagents\Scripts\Activate.ps1`
  - `pip install --upgrade pip`
  - `pip install mlagents==0.28.0`
  - `pip install torch==2.7.0`
  - `pip install protobuf==3.20.0`
- **결과 3:** 다시 `TypeError: Invalid first argument to register()...` 발생. Python 3.9와 `mlagents` 0.28.0 조합의 근본적인 호환성 문제로 판단.

## 문제 5: `cattrs` 버전 호환성 의심

- **증상:** 깨끗한 Python 3.9 환경에서도 `TypeError` 지속 발생.
- **시도 4:** `cattrs` 라이브러리 버전을 이전 버전(1.1.0)으로 다운그레이드.
  ```bash
  pip install cattrs==1.1.0
  ```
- **결과 4:** 동일한 `TypeError` 발생. `cattrs` 버전 문제는 아닌 것으로 추정.

## 최종 결론 및 해결책 모색

- **웹 검색:** GitHub 이슈 [#5912](https://github.com/Unity-Technologies/ml-agents/issues/5912) 등에서 동일한 `TypeError`를 Python 3.9 환경에서 겪는 사례 확인.
- **권장 해결책:** 다수의 사용자가 **Python 3.8.x 버전**으로 환경을 구성했을 때 해당 `TypeError`가 해결되었다고 보고함.
- **다음 단계:** Python 3.8.x 버전을 설치하고, 새로운 가상 환경(`.venv-mlagents-py38`)을 생성하여 패키지를 설치한 후 학습 재시도. 