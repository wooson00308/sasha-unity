# 07. Python 환경 설정 지옥 체험기 (Unity ML-Agents v3.0.0 / Python release_21)

**결론: 포기! 현 시점(2025년 5월) 윈도우 환경에서는 설정이 거의 불가능에 가까움.**

Unity ML-Agents 패키지 v3.0.0 버전을 사용하기 위해 해당 버전에 맞는 Python 환경(`ml-agents` GitHub 저장소의 `release_21` 태그, 대략 v1.0.0 이상 버전)을 설정하려 했으나, 끔찍한 의존성 문제와 빌드 오류의 연속으로 결국 포기하고 이전 버전(v2.0.1)으로 회귀하기로 결정했다.

우리가 겪었던 주요 문제점들은 다음과 같다:

1.  **애매모호한 Python 버전 요구사항:**
    *   Unity 패키지 v3.0.0은 GitHub `release_21` 태그에 해당하는 Python 패키지를 요구한다. 이 버전은 PyPI에는 정식 릴리스되지 않아 `git+https`로 설치해야 했다.
    *   `release_21`은 **Python 3.10.1 ~ 3.10.12** 버전을 요구한다. (그 이상/이하 버전은 호환 안 됨)

2.  **NumPy 1.21.x 빌드 지옥:**
    *   `release_21`은 `numpy~=1.21.2` 버전을 요구한다.
    *   Python 3.10 환경(venv, Anaconda 모두)에서 `pip`로 NumPy 1.21.x 버전을 설치하려고 하면, **소스 코드 빌드를 시도하며 실패**한다.
    *   주요 에러: `TypeError: CCompiler_spawn() got an unexpected keyword argument 'env'`
    *   이 에러는 `setuptools`, MSVC 컴파일러, Python 버전 간의 복잡한 호환성 문제로 보이며, `SETUPTOOLS_USE_DISTUTILS='stdlib'` 환경 변수 설정 등의 꼼수도 통하지 않았다.
    *   **미리 컴파일된 Wheel 파일 부재:** 공식 PyPI에는 Python 3.10 윈도우(x64)용 NumPy 1.21.2 휠 파일이 **없다**. (이후 버전인 1.21.6은 존재하여 겨우 설치 성공)
    *   Christoph Gohlke의 비공식 저장소에서도 해당 버전(+mkl 포함)을 찾기 어려웠다.

3.  **끝나지 않는 의존성 지옥 (`mlagents_envs` -> `pettingzoo`):**
    *   NumPy 문제를 해결하고 `ml-agents` 설치를 시도하자, 이번엔 의존성 패키지인 `mlagents_envs` 설치에서 막혔다.
    *   `mlagents_envs` (v1.0.0)는 `pettingzoo==1.15.0` 버전을 요구한다.
    *   **PyPI에 해당 버전이 존재함에도 불구**하고, `pip`는 `No matching distribution found for pettingzoo==1.15.0` 에러를 뱉어냈다.
    *   `pip` 버전 업데이트, 캐시 삭제, `--no-binary` 옵션을 통한 강제 소스 빌드 시도 등 **모든 방법이 실패**했다.

4.  **시간 낭비와 좌절:**
    *   수많은 환경 재구성, 패키지 재설치, 버전 조합 테스트에 엄청난 시간을 쏟아부었지만 결국 환경 설정에 실패했다.
    *   이는 단순히 특정 패키지 하나의 문제가 아니라, Unity ML-Agents `release_21` 버전과 Python 생태계의 특정 시점(Python 3.10, NumPy 1.21, pettingzoo 1.15 등) 간의 **총체적인 호환성 난국**으로 판단된다.

**결론:**

이 고통스러운 과정을 통해 얻은 교훈은, **최신 버전이라고 무조건 좋은 것이 아니며, 특히 복잡한 의존성을 가진 머신러닝 프레임워크의 경우 안정성이 검증된 버전을 사용하는 것이 현명하다**는 것이다.

따라서 Unity ML-Agents 패키지를 **v2.0.1**로 다운그레이드하고, 이전에 호환성이 확인되었던 Python 환경(`mlagents==0.28.0`, `torch==1.10.1`, Python 3.8)을 사용하는 것으로 방향을 전환한다. 