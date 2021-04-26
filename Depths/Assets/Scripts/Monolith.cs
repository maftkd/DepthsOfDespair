using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.AI;

public class Monolith : MonoBehaviour
{
	Camera _main;
	public Camera _introCam;
	Transform _mainTransform;
	Transform _cameraTarget;
	float _moveSpeed=6f;
	float _lookSpeed=1f;
	float _cameraSmooth=5f;
	float _moveTimer=0;
	Transform _arms;
	Hand _left;
	Hand _right;
	bool _attackToggle;
	float _attackTimer;
	public Transform _ghost;
	List<Ghost> _ghosts;
	public Transform [] _ghostWaves;
	public LayerMask _playerMask;
	public LayerMask _bossMask;
	int _hp = 1;
	float _hitCooldown=0;
	public Transform _fireball;
	List<Fireball> _fireballs;
	bool _spawnStarted;
	bool _secondWave;
	int _ghostPoints;
	int _totesGhosts;
	public float _hitTimer;
	int _bossButtons;
	public Transform _bossPrefab;
	Ghost _boss;
	Image _bossHealth;
	int _inputState; //0 = default, 1=movement disabled
	public bool _hasPowers;
	Canvas _reticle;
	public Transform[] _lampParents;
	List<Lamp> _lamps;
	int[] _lampSequence;
	int _curLamp;
	[HideInInspector]
	public float _fade=1;
	bool _gameEnd;

	//options
	Canvas _options;
	Slider _sens;
	Slider _vol;
	float _sensMult=1;
	Button _exit;
	public AudioMixer _mixer;

	//audio sources
	AudioSource _action;
	AudioSource _land;
	AudioSource _hit;
	AudioSource _fail;
	AudioSource _powerChords;
	AudioSource _lightGuitar;
	AudioSource _percussion;
	AudioSource _celestial;
	AudioSource _infernal;

	//Animation curves
	public AnimationCurve _easeIn;

	class Lamp{
		public Transform _t;
		ParticleSystem _ps;
		int _index;

		public Lamp(Transform t,int i){
			_t=t;
			_ps = _t.GetComponent<ParticleSystem>();
			_index=i;
		}

		public void Activate(bool act,Monolith m){
			if(act && !_ps.isPlaying)
			{
				_ps.Play();
				m.LampActivated(_index);
			}
			else if(!act)
				_ps.Stop();
			//hm
		}
	}

	class Fireball{
		public Transform _t;
		public float _timer;
		Vector3 _dir;
		float _speed;
		Collider[] _results;
		bool _physics;

		public Fireball(Transform prefab, Vector3 pos,Vector3 dir){
			_t = Instantiate(prefab,pos+dir*.5f,Quaternion.identity);
			_dir=dir;
			_timer=0;
			_speed=20f;
			_t.GetComponent<AudioSource>().pitch=Random.Range(1.0f,1.5f);
			_results = new Collider[2];
			_physics=true;
		}

		public void Update(Monolith m){
			_timer+=Time.deltaTime;
			if(_timer>3f)
				return;
			_t.position+=_dir*_speed*Time.deltaTime;
			if(m._ghosts.Count>0){
				foreach(Ghost g in m._ghosts){
					if(g._hp>0 && (g._transform.position+Vector3.up-_t.position).sqrMagnitude<1f)
					{
						g.Hit(null);
						_timer=3f;
						return;
					}
				}
			}
			else if(m._boss!=null && m._boss._hp>0 && _physics==true){
				//check if fireball hits boss's shell
				int hits=0;
				hits = Physics.OverlapSphereNonAlloc(_t.position,0.25f,_results,m._bossMask,QueryTriggerInteraction.UseGlobal);
				if(hits>0){
					_physics=false;
					Vector3 closestPoint = _results[0].ClosestPoint(_t.position);
					Vector3 newDir=(_t.position-closestPoint).normalized;
					float dt = Vector3.Dot(newDir,Vector3.down);
					if(dt>0.9f){
						//hit belly
						m._boss.Hit(m,5);
						_physics=false;
					}
					else{
						//bounce off
						m._boss.Hit(m,1,false);
						_dir=newDir;
					}
				}
			}
			else if(m._lamps.Count>0){
				foreach(Lamp l in m._lamps){
					if((l._t.position-_t.position).sqrMagnitude<9f){
						l.Activate(true,m);
						_timer=3f;
						return;
					}
				}
			}
		}
	}

	class Ghost{
		public Transform _transform;
		int _state;
		public int _hp;
		int _startHp;
		NavMeshAgent _nma;
		Vector3 _startPos;
		Vector3 _targetPos;
		float _pounceTimer;
		float _sleepTime;
		float _walkSpeed;
		float _pounceSpeed;
		float _pounceDistSqr;
		float _hitTimer;
		Material _mat;
		AudioSource _audio;
		bool _isBoss;
		float _sleepTimer;
		float _sleepDur;
		float _hitRadiusSqr;

		public Ghost(Transform prefab, Vector3 pos,bool isBoss=false){
			_isBoss=isBoss;
			_transform = Instantiate(prefab,pos,Quaternion.identity);
			_nma = _transform.GetComponent<UnityEngine.AI.NavMeshAgent>();
			_state=isBoss?-1 : 3;
			_hp=isBoss?200 : 2;
			_startHp=_hp;
			_pounceTimer=0;
			_sleepTime = Random.value*2;
			_startPos=Vector3.zero;
			_targetPos=Vector3.zero;
			_walkSpeed=isBoss?3f :6f;
			_pounceSpeed=isBoss?1f :10f;
			_pounceDistSqr=isBoss?80f : 100f;
			_mat = _transform.GetComponent<MeshRenderer>().material;
			_hitTimer=2f;
			_audio = _transform.GetComponent<AudioSource>();
			//play some audio
			_audio.pitch=Random.Range(0.8f,1.2f);
			_audio.Play();
			_sleepTimer=0;
			_sleepDur=0;
			_hitRadiusSqr=_isBoss?80f : 2f;
		}

		public void Spawn(){
			_state=3;
		}

		public void Update(Monolith m){
			//check hit
			if(_hitTimer<1f){
				_hitTimer+=Time.deltaTime;
				_mat.SetColor("_Color",Color.Lerp(Color.white,Color.red,
							Mathf.Round(Mathf.PingPong(_hitTimer,0.1f)*10)));
			}
			//rotate
			if(_state==2||_state==5)
				_transform.Rotate(Vector3.up*Time.deltaTime*540f);
			else if(_state==6)
				_transform.Rotate(Vector3.down*Time.deltaTime*60f);
			else
				_transform.Rotate(Vector3.up*Time.deltaTime*120f);
			//check player collision
			if(_state!=4&&_state!=5&&(m.transform.position-_transform.position).sqrMagnitude<_hitRadiusSqr){
				if(Mathf.Abs(m.transform.position.y-_transform.position.y)<1f)
					m.PlayerHit();
			}
			if(_hp>-1){
				switch(_state){
					case 0://look for LOS
						RaycastHit hit;
						if(Physics.Raycast(_transform.position+Vector3.up,
									m.transform.position-_transform.position, out hit, 50f,
									m._playerMask)){
							if(hit.transform==m.transform&&
									Vector3.Dot(m._mainTransform.forward,
										(_transform.position-m.transform.position).normalized)>0.5f){
								_nma.enabled=true;
								_nma.SetDestination(m.transform.position);
								_nma.speed=_walkSpeed;
								_state=1;
								_sleepDur=Random.Range(2f,4f);
							}
						}
						//cast ray from eyes towards player
						//if hit player
						//activate nma
						//go to state 1
						break;
					case 1://get into position
						_nma.SetDestination(m.transform.position);
						_sleepTimer+=Time.deltaTime;
						if(_isBoss && _sleepTimer>_sleepDur){
							Tackle();
							break;
						}
						if((m.transform.position-_transform.position).sqrMagnitude<=_pounceDistSqr)
						{
							if(_isBoss){//tackle
								//Tackle();
								break;
							}
							_state=2;
							_pounceTimer=0;
							_nma.speed=_pounceSpeed;
							//play some audio
							_audio.Stop();
							_audio.pitch=Random.Range(0.8f,1.2f);
							_audio.Play();
						}
						break;
					case 2://pounce
						_pounceTimer+=Time.deltaTime;
						//Vector3 pos = _transform.position;
						//pos.y = Mathf.Lerp(_startPos.y,_startPos.y+2f,1-Mathf.Abs(1-_pounceTimer));
						//_transform.position=pos;
						if(_pounceTimer>2f){
							_state=3;
							_sleepTime = Random.value*2;
						}
						else{
						}
						break;
					case 3://rest
						_pounceTimer+=Time.deltaTime;
						if(_pounceTimer>2+_sleepTime){
							_state=0;
							//Debug.Log("Lookin for player");
						}
						break;
					case 4://die
						_pounceTimer+=Time.deltaTime;
						Vector3 scale = Vector3.one;
						scale.y=1-_pounceTimer;
						_transform.localScale=scale;
						if(_pounceTimer>1f)
						{
							m._ghostPoints++;
							_hp=-1;
						}
						break;
					case 5://tackle
						_sleepTimer+=Time.deltaTime;
						if(_sleepTimer>_sleepDur){
							Descend(m);
							break;
						}
						_transform.position=Vector3.Lerp(_transform.position,
								m.transform.position+Vector3.up*7f,
								Time.deltaTime*0.8f);
						break;
					case 6://descend
						_sleepTimer+=Time.deltaTime;
						if(_sleepTimer<0.5f){
							//wait
						}
						else{
						//descend
						_transform.position=Vector3.Lerp(_startPos,_targetPos,
								m._easeIn.Evaluate(_sleepTimer-0.5f));
						}

						if(_sleepTimer>_sleepDur){
							m._land.Play();
							//rest
							_state=3;
							_sleepTime = Random.Range(3f,4f);
							_pounceTimer=2f;
							break;
						}
						break;
					default://do nothing - used for animation
						break;
				}
			}
		}

		public void Hit(Monolith m,int amount=1,bool colorChange=true){
			_hp-=amount;
			if(_isBoss){
				m._bossHealth.fillAmount=Mathf.Max(0,_hp/(float)_startHp);
			}
			else
				_nma.enabled=false;
			if(colorChange)
				_hitTimer=0f;
			if(_hp<=0)
			{
				if(!_isBoss){
					_hp=0;
					_state=4;
					_pounceTimer=0;
				}
				else{
					//boss dead
					Debug.Log("Boss dead");
					m.EndGame();
					_state=-1;
				}
			}
			else if(!_isBoss){
				_state=3;
				_pounceTimer=2;
				_sleepTime = Random.value*2;
			}
		}

		public void Die(){
			_hp=-1;
		}

		void Tackle(){
			_state=5;
			_nma.enabled=false;
			_sleepTimer=0;
			_sleepDur=Random.Range(2f,4f);
		}

		void Descend(Monolith m){
			_state=6;
			_sleepTimer=0;
			_sleepDur=1.5f;
			_startPos=_transform.position;
			/*
			RaycastHit hit;
			if(Physics.Raycast(_transform.position,
						Vector3.down, out hit, 50f,
						lm)){
				_targetPos=hit.point;
			}
			*/
			_targetPos=_startPos;
			_targetPos.y=m.transform.position.y;
			//_targetPos=m.transform.position;
		}
	}

	class Hand{
		Animator _anim;
		Transform _wrist;
		Transform _parent;
		ParticleSystem _fireParts;

		public Hand(Transform parent, float off){
			_parent = parent;
			_anim = _parent.GetComponent<Animator>();
			_anim.SetFloat("offset",off);
			_wrist = _parent.Find("wrist.R");
			_fireParts = _parent.GetComponentInChildren<ParticleSystem>();
		}

		public void Show(bool show){
			_anim.SetBool("visible",show);
			_fireParts.Play();
		}

		public void Attack(bool attack){
			_anim.SetBool("visible",false);
			_anim.SetTrigger(attack?"attack":"coil");
			if(attack)
			{
				_fireParts.Stop();
			}
			else
				_fireParts.Play();

		}

	}
    // Start is called before the first frame update
    void Start()
    {
		foreach(Transform t in _ghostWaves){
			foreach(Transform c in t){
				_totesGhosts++;
			}
		}
		_arms = GameObject.Find("Arms").transform;
		_right = new Hand(_arms.GetChild(0).GetChild(0),0);
		_left = new Hand(_arms.GetChild(0).GetChild(1),0.25f);
		_cameraTarget = transform.GetChild(0);
		_main = Camera.main;
		_mainTransform = _main.transform;
		_cameraTarget.position = _mainTransform.position;
		Shader.SetGlobalColor("_AmbientColor",_main.backgroundColor);
		Shader.SetGlobalVector("_Screen", new Vector4(Screen.width,Screen.height,1f/Screen.width,1f/Screen.height));

		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible=false;

		Transform musicMan = transform.GetChild(1);
		Transform sfxMan = transform.GetChild(2);
		_action = sfxMan.GetChild(0).GetComponent<AudioSource>();
		_land = sfxMan.GetChild(1).GetComponent<AudioSource>();
		_hit = sfxMan.GetChild(2).GetComponent<AudioSource>();
		_fail = sfxMan.GetChild(3).GetComponent<AudioSource>();

		_powerChords = musicMan.GetChild(3).GetComponent<AudioSource>();
		_lightGuitar = musicMan.GetChild(0).GetComponent<AudioSource>();
		_percussion = musicMan.GetChild(1).GetComponent<AudioSource>();
		_celestial = musicMan.GetChild(2).GetComponent<AudioSource>();
		_infernal = musicMan.GetChild(4).GetComponent<AudioSource>();
		_reticle = GameObject.Find("Reticle").GetComponent<Canvas>();
		if(_hasPowers){
			_left.Show(true);
			_right.Show(true);
			_reticle.enabled=true;
		}
		else
			_reticle.enabled=false;

		_fireballs = new List<Fireball>();
		_ghosts = new List<Ghost>();
		_lamps = new List<Lamp>();
		int counter=0;
		foreach(Transform t in _lampParents){
			_lamps.Add(new Lamp(t,counter));
			counter++;
		}

		_lampSequence = new int[4];
		for(int i=0; i<4; i++)
			_lampSequence[i]=-1;
		_curLamp=0;

		if(_introCam!=null){
			_introCam.enabled=true;
			_introCam.GetComponent<AudioListener>().enabled=true;
			_main.enabled=false;
			_main.GetComponent<AudioListener>().enabled=false;
			_inputState=2;
			StartCoroutine(IntroAnimationR());
		}

		_options = GameObject.Find("OptionsCanvas").GetComponent<Canvas>();
		_sens = _options.transform.Find("Sensitivity").GetComponent<Slider>();
		_vol = _options.transform.Find("Volume").GetComponent<Slider>();
		_exit = _options.transform.Find("Exit").GetComponent<Button>();
		_sens.onValueChanged.AddListener(delegate {SetSensitivity();});
		_vol.onValueChanged.AddListener(delegate {SetVolume();});
		_exit.onClick.AddListener(delegate {Application.Quit();});
    }

	void SetSensitivity(){
		_sensMult=_sens.value;
	}

	void SetVolume(){
		_mixer.SetFloat("Volume",_vol.value);
	}

    // Update is called once per frame
    void Update()
    {
		//look + move
		if(_inputState<2){
			Vector2 look = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
			_mainTransform.Rotate(Vector3.up,look.x*_lookSpeed*_sensMult);
			_mainTransform.Rotate(transform.right,look.y*_lookSpeed*_sensMult);
			//level out the z (roll component)
			Vector3 eulers = _mainTransform.localEulerAngles;
			eulers.z = 0;
			_mainTransform.localEulerAngles = eulers;
		}
		if(_inputState<1){
			Vector2 movement = new Vector2(Input.GetAxis("Horizontal"),Input.GetAxis("Vertical"));
			if(movement.sqrMagnitude==0)
			{
				_moveTimer-=Time.deltaTime*.5f;
				_moveTimer = Mathf.Clamp01(_moveTimer);
			}
			else{
				if(movement.sqrMagnitude>1)
				{
					_moveTimer+=Time.deltaTime*.5f;
					_moveTimer = Mathf.Clamp01(_moveTimer);
					movement.Normalize();
				}
				else{
					_moveTimer-=Time.deltaTime*.5f;
					_moveTimer = Mathf.Clamp01(_moveTimer);
				}
			}
			Vector3 flatForward = _mainTransform.forward;
			flatForward.y=0;
			flatForward.Normalize();
			Vector3 flatRight = _mainTransform.right;
			flatRight.y=0;
			flatRight.Normalize();

			transform.position+=(flatForward*movement.y+flatRight*movement.x)*Time.deltaTime*_moveSpeed;

			_mainTransform.position = Vector3.Lerp(_mainTransform.position,_cameraTarget.position,
					_cameraSmooth*Time.deltaTime);
		}
		/*
		_arms.position = Vector3.Lerp(_arms.position,_mainTransform.position,
				Mathf.Lerp(_armSmooth*Time.deltaTime,1f,_moveTimer));
				*/
		_arms.position = _mainTransform.position;
		_arms.rotation = _mainTransform.rotation;

		if(_hasPowers){
			_attackTimer+=Time.deltaTime;
			if(_attackTimer>2f &&_attackTimer<3f){
				_left.Show(true);
				_right.Show(true);
				_attackTimer=4f;
			}
			else if(_attackTimer>7f && _attackTimer<8f){
				_left.Show(false);
				_right.Show(false);
				_attackTimer=8f;
			}
			if(_attackTimer>0.2f && (Input.GetMouseButtonDown(0) || Input.GetButtonDown("Submit"))){
				_left.Attack(_attackToggle);
				_right.Attack(!_attackToggle);
				_attackToggle=!_attackToggle;
				_fireballs.Add(new Fireball(_fireball,_mainTransform.position,_mainTransform.forward));
				_attackTimer=0;
			}
		}


		//ghost updates
		for(int i=_ghosts.Count-1; i>=0; i--){
			_ghosts[i].Update(this);
			if(_ghosts[i]._hp<0){
				Destroy(_ghosts[i]._transform.gameObject);
				_ghosts.Remove(_ghosts[i]);
				if(_ghosts.Count<=0){
					GhostsDefeated();
				}
			}
		}


		//update fireballs
		for(int i=_fireballs.Count-1; i>=0; i--){
			_fireballs[i].Update(this);
			if(_fireballs[i]._timer>3f&&_fireballs[i]._timer<5f){
				_fireballs[i]._timer=10f;
				_fireballs[i]._t.GetComponent<AudioSource>().Play();
				Destroy(_fireballs[i]._t.gameObject,1f);
				_fireballs.Remove(_fireballs[i]);
			}
		}

		//boss update
		if(_boss!=null&&!_gameEnd)
			_boss.Update(this);

		_hitCooldown+=Time.deltaTime;
		if(_hitTimer>0)
		{
			_hitTimer-=Time.deltaTime*.5f;
			if(_hitTimer<0)
				_hitTimer=0;
			if(_hitTimer>5f){
				Debug.Log("Game ovahh");
			}
		}

		if(Input.GetButtonDown("Cancel")){
			_options.enabled = !_options.enabled;
			if(_options.enabled==true){
				Cursor.lockState = CursorLockMode.None;
				Cursor.visible=true;
			}
			else{
				Cursor.lockState = CursorLockMode.Locked;
				Cursor.visible=false;
			}
		}
    }

	void OnTriggerEnter(Collider other){
		if(other.gameObject.name=="Doorway"){
			other.gameObject.GetComponent<AudioSource>().Play();
			StartCoroutine(FadeToSceneR("Puzzle"));
			//Debug.Log("Ready to gooo");
			//todo transition
			//play some rockslide sounds
		}
		else if(other.gameObject.name=="Book"){
			Destroy(other.gameObject);
			Debug.Log("got ze book");
			_hasPowers=true;
			_left.Show(true);
			_right.Show(true);
			_reticle.enabled=true;
			StartCoroutine(RampUpAudioR(_celestial));
			_action.Play();
		}
		else if(other.gameObject.name=="Portal"){
			//todo transition
			Debug.Log("Ready to gooo");
			StartCoroutine(FadeToSceneR("Stairs"));
		}
		else if(other.gameObject.name=="StairBottom"){
			//todo transition
			Debug.Log("Ready to goooo");
			StartCoroutine(FadeToSceneR("Gamefeel"));
		}
	}

	bool _descentStarted;
	void OnCollisionEnter(Collision other){
		if(other.gameObject.name=="SpawnButton" && 
				transform.position.y>other.transform.position.y+other.transform.localScale.y*.45f){
			if(!_spawnStarted){
				_spawnStarted=true;
				StartCoroutine(ButtonPressR(other.transform,0,10f));
			}
			else if(!_secondWave && _ghostPoints>=_ghostWaves[0].childCount){
				_secondWave=true;
				StartCoroutine(ButtonPressR(other.transform,1,2f));
			}
			else if(!_descentStarted && _ghostPoints>=_totesGhosts){
				_descentStarted=true;
				StartCoroutine(DescendR(other.transform));
			}
		}
		else if(other.gameObject.name=="BossSpawn"){
			StartCoroutine(ButtonPressR(other.transform,-1,1f,1f));
			other.gameObject.name="Pressed";
			_bossButtons++;
			if(_bossButtons==4&&_boss==null){
				SpawnBoss();
			}
		}
	}

	IEnumerator DescendR(Transform t){
		t.GetComponent<AudioSource>().Play();
		float timer=0;
		while(timer<8f){
			timer+=Time.deltaTime;
			t.position+=Vector3.down*Time.deltaTime*0.5f;
			yield return null;
		}
		t.GetComponent<AudioSource>().Stop();
		StartCoroutine(FadeToSceneR("Boss"));
		//todo transition
	}

	IEnumerator ButtonPressR(Transform t,int wave,float rockTime,float speed=0.5f){
		t.GetComponent<AudioSource>().pitch=Random.Range(1.2f,1.5f);
		t.GetComponent<AudioSource>().Play();
		float timer=0;
		if(wave>=0)
			SpawnGhosts(wave);
		while(timer<rockTime){
			timer+=Time.deltaTime;
			t.position+=Vector3.down*Time.deltaTime*speed;
			yield return null;
		}
		t.GetComponent<AudioSource>().Stop();
	}

	IEnumerator RampUpAudioR(AudioSource a){
		float timer=0;
		while(a.volume<0.3f){
			timer+=Time.deltaTime;
			a.volume = timer/10f;
			yield return null;
		}
	}

	IEnumerator RampDownAudioR(AudioSource a){
		float timer=3;
		while(a.volume>0){
			timer-=Time.deltaTime;
			a.volume = timer/10f;
			yield return null;
		}
		a.volume = 0f;
	}

	public void SpawnGhosts(int wave){
		foreach(Transform t in _ghostWaves[wave])
			_ghosts.Add(new Ghost(_ghost,t.position));
		_action.Play();
		if(_rampDown!=null)
			StopCoroutine(_rampDown);
		StartCoroutine(RampUpAudioR(_powerChords));
	}

	public void SpawnBoss(){
		_action.Play();
		//StartCoroutine(RampUpAudioR(_powerChords));
		_boss = new Ghost(_bossPrefab,GameObject.Find("BossWake").transform.position,true);
		StartCoroutine(BossEnterR());
		StartCoroutine(RampUpAudioR(_infernal));
		Transform healthBar = GameObject.Find("HealthBar").transform;
		healthBar.GetComponent<Canvas>().enabled=true;
		_bossHealth = healthBar.GetComponentInChildren<Image>();
		_bossHealth.fillAmount=1f;
	}

	IEnumerator BossEnterR(){
		Vector3 wake=GameObject.Find("BossWake").transform.position;
		Vector3 end = GameObject.Find("BossEnter").transform.position;
		Vector3 fight = GameObject.Find("BossFight").transform.position;
		float timer=0;
		while(timer<3f){
			timer+=Time.deltaTime;
			_boss._transform.position=Vector3.Lerp(wake,end,timer/3f);
			yield return null;
		}
		timer=0;
		while(timer<1f){
			timer+=Time.deltaTime;
			_boss._transform.position = Vector3.Lerp(end,fight,_easeIn.Evaluate(timer));
			yield return null;
		}
		_land.Play();
		_boss.Spawn();
		//StartCoroutine(RampUpAudioR(_infernal));
		StartCoroutine(RampUpAudioR(_powerChords));
	}

	public void PlayerHit(){
		if(_hitCooldown>2f){
			_hitTimer+=3f;
			_hp--;
			_hitCooldown=0;
			_hit.Play();
		}
	}

	IEnumerator _rampDown;
	public void GhostsDefeated(){
		_rampDown = RampDownAudioR(_powerChords);
		StartCoroutine(_rampDown);
		//StartCoroutine(RampDownAudioR(_powerChords));
		//activate elevator
		//some other things
	}

	public void LampActivated(int index){
		Debug.Log("Activated lamp "+index);
		_lampSequence[_curLamp]=index;
		_curLamp++;
		if(_curLamp==4){
			//correct pattern = 0,2,1,3
			Debug.Log("Checking pattern");
			for(int i=0; i<4; i++){
				Debug.Log(_lampSequence[i]);
			}
			if(_lampSequence[0]==0&&_lampSequence[1]==2&&_lampSequence[2]==1&&_lampSequence[3]==3){
				StartCoroutine(PuzzleSuccessR());
			}
			else{
				StartCoroutine(PuzzleFailedR());
			}
		}
	}

	IEnumerator PuzzleSuccessR(){
		Transform door = GameObject.Find("Door").transform;
		float timer=0;
		door.GetComponent<AudioSource>().Play();
		while(timer<6f){
			timer+=Time.deltaTime;
			door.position+=Vector3.down*Time.deltaTime;
			yield return null;
		}
	}

	IEnumerator PuzzleFailedR(){
		_fail.Play();
		yield return new WaitForSeconds(1f);
		for(int i=0; i<4; i++)
			_lampSequence[i]=-1;
		_curLamp=0;
		foreach(Lamp l in _lamps)
			l.Activate(false,null);
	}

	public void EndGame(){
		_gameEnd=true;
		StartCoroutine(RampDownAudioR(_powerChords));
		StartCoroutine(RampDownAudioR(_infernal));
		StartCoroutine(RampDownAudioR(_lightGuitar));
		StartCoroutine(RampDownAudioR(_percussion));
		StartCoroutine(RampDownAudioR(_celestial));
		StartCoroutine(EndGameR());
	}

	IEnumerator EndGameR(){
		ParticleSystem ps = GameObject.Find("EndParts").GetComponent<ParticleSystem>();
		ps.transform.SetParent(_boss._transform);
		ps.transform.localPosition=Vector3.up;
		ps.Play();
		float timer=0;
		while(timer<10){
			timer+=Time.deltaTime;
			_fade = 1-timer/10f;
			yield return null;
		}
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying=false;
#else
		Application.Quit();
#endif
	}

	IEnumerator IntroAnimationR(){
		//animate intro cam
		Transform t = _introCam.transform;
		Vector3 startPos = t.position;
		Vector3 endPos = _mainTransform.position;
		Quaternion startRot = t.rotation;
		Quaternion endRot = _mainTransform.rotation;
		float timer=0; 
		float dur=10f;
		AnimationCurve curve = AnimationCurve.EaseInOut(0f,0f,dur,1f);
		while(timer<dur){
			timer+=Time.deltaTime;
			t.position = Vector3.Lerp(startPos,endPos,curve.Evaluate(timer));
			t.rotation = Quaternion.Slerp(startRot,endRot,curve.Evaluate(timer));
			yield return null;
		}
		t.position=endPos;
		t.rotation=endRot;
		//switch cams back
		_introCam.enabled=false;
		_introCam.GetComponent<AudioListener>().enabled=false;
		_main.enabled=true;
		_main.GetComponent<AudioListener>().enabled=true;
		_inputState=0;
	}

	IEnumerator FadeToSceneR(string s){
		float timer=0;
		float dur=3f;
		while(timer<dur){
			timer+=Time.deltaTime;
			_fade = 1-timer/dur;
			yield return null;
		}
		SceneManager.LoadScene(s);
	}

	//reminder - comment this out before build
	Transform _bossPoints;
	/*
	void OnDrawGizmos(){
		int counter=0;
		foreach(Transform t in _ghostWaves){
			switch(counter){
				case 0:
					Gizmos.color = Color.red;
					break;
				case 1:
					Gizmos.color = Color.yellow;
					break;
				default:
					Gizmos.color = Color.magenta;
					break;
			}
			/*
			foreach(Transform c in t){
				Gizmos.DrawSphere(c.position,0.5f);
			}
			counter++;
		}
	}
	*/
}
