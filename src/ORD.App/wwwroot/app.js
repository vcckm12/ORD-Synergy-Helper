let allUnits = [];
let currentDeckIds = [];
let activeTier = 'ALL';

// 페이지 초기 로드
document.addEventListener('DOMContentLoaded', async () => {
  await loadUnits();
  // 기본 데모 덱 로드 (루피 초월)
  loadPreset('luffy');
});

// 전체 유닛 목록 로드
async function loadUnits() {
  try {
    const res = await fetch('/api/units');
    allUnits = await res.json();
    renderUnitPicker();
  } catch (err) {
    console.error('Failed to load units:', err);
  }
}

// 덱 평가 및 추천 요청
async function evaluateDeck() {
  try {
    const res = await fetch('/api/evaluate', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ unitIds: currentDeckIds, targetRound: 65 })
    });

    const data = await res.json();
    updateDashboardUI(data);
  } catch (err) {
    console.error('Failed to evaluate deck:', err);
  }
}

// UI 업데이트
function updateDashboardUI(data) {
  const { analysis, recommendations, currentUnits } = data;

  // 1. 메인 캐리 및 덱 타입
  const carryNameEl = document.getElementById('mainCarryName');
  const deckTypeBadge = document.getElementById('deckTypeBadge');
  const scoreValEl = document.getElementById('scoreValue');

  if (analysis.mainCarry) {
    carryNameEl.textContent = analysis.mainCarry.name;
    deckTypeBadge.textContent = analysis.primaryType === 1 ? '물리 덱 (물딜)' : '마법 덱 (마딜)';
    deckTypeBadge.className = analysis.primaryType === 1 ? 'badge badge-physical' : 'badge badge-magic';
  } else {
    carryNameEl.textContent = '- 필드에 유닛이 없습니다 -';
    deckTypeBadge.textContent = '덱 미정';
    deckTypeBadge.className = 'badge';
  }

  // 점수
  scoreValEl.textContent = Math.round(analysis.nightmareClearProbabilityScore);
  if (analysis.nightmareClearProbabilityScore >= 90) {
    scoreValEl.style.color = '#10b981'; // 초록
  } else if (analysis.nightmareClearProbabilityScore >= 60) {
    scoreValEl.style.color = '#f59e0b'; // 주황
  } else {
    scoreValEl.style.color = '#ef4444'; // 빨강
  }

  // 2. 4대 게이지
  // 방깎 (목표 211)
  const armorValEl = document.getElementById('armorVal');
  const armorBar = document.getElementById('armorBar');
  const armorSub = document.getElementById('armorSub');
  const totalArmor = analysis.totalArmorReduction;
  armorValEl.textContent = totalArmor;
  const armorPct = Math.min(100, (totalArmor / 211) * 100);
  armorBar.style.width = `${armorPct}%`;
  armorSub.textContent = `기본 오라 ${analysis.totalArmorReduction} + 암브 ${analysis.totalArmorBreak} (v2.323 목표 211)`;

  // 이감 (목표 102%)
  const slowValEl = document.getElementById('slowVal');
  const slowBar = document.getElementById('slowBar');
  const slowSub = document.getElementById('slowSub');
  slowValEl.textContent = `${analysis.totalSlow}%`;
  const slowPct = Math.min(100, (analysis.totalSlow / 102) * 100);
  slowBar.style.width = `${slowPct}%`;
  if (analysis.totalSlow >= 102) {
    slowSub.textContent = `★ 풀이감 102% 완결! (몹 완벽 고정)`;
    slowSub.style.color = '#38bdf8';
  } else {
    slowSub.textContent = `풀이감 102%까지 ${102 - analysis.totalSlow}% 필요`;
    slowSub.style.color = '#94a3b8';
  }

  // 스턴 (목표 2.0)
  const stunValEl = document.getElementById('stunVal');
  const stunBar = document.getElementById('stunBar');
  const stunSub = document.getElementById('stunSub');
  stunValEl.textContent = analysis.totalStun.toFixed(1);
  const stunPct = Math.min(100, (analysis.totalStun / 2.0) * 100);
  stunBar.style.width = `${stunPct}%`;
  if (analysis.totalStun < 2.0) {
    stunSub.textContent = `위험: 최소 2.0 스턴 필요`;
    stunSub.style.color = '#f87171';
  } else if (analysis.totalStun <= 2.5) {
    stunSub.textContent = `적정: 2.0~2.5 안정권 스턴`;
    stunSub.style.color = '#10b981';
  } else {
    stunSub.textContent = `주의: 2.5 초과 스턴 과투자 (딜로스 주의)`;
    stunSub.style.color = '#f59e0b';
  }

  // 3. 결손(Deficit) 리스트
  const deficitsList = document.getElementById('deficitsList');
  deficitsList.innerHTML = '';
  if (analysis.deficits && analysis.deficits.length > 0) {
    analysis.deficits.forEach(d => {
      const li = document.createElement('li');
      li.textContent = d;
      if (d.includes('치명적') || d.includes('위험')) {
        li.className = 'critical';
      } else if (d.includes('달성')) {
        li.className = 'success';
      }
      deficitsList.appendChild(li);
    });
  } else {
    deficitsList.innerHTML = '<li class="success">모든 악몽 클리어 시너지가 충족되었습니다!</li>';
  }

  // 4. AI 추천 리스트
  const recListEl = document.getElementById('recList');
  recListEl.innerHTML = '';
  if (recommendations && recommendations.length > 0) {
    recommendations.slice(0, 4).forEach((r, idx) => {
      const item = document.createElement('div');
      item.className = `rec-item rank-${idx + 1}`;
      item.innerHTML = `
        <div class="rec-left">
          <span class="rec-rank-badge">#${idx + 1}</span>
          <div>
            <div>
              <span class="rec-unit-name">${r.targetUnit.name}</span>
              <span class="rec-tier-badge">${tierName(r.targetUnit.tier)}</span>
            </div>
            <div class="rec-reason">${r.coreReason}</div>
          </div>
        </div>
        <div class="rec-right">
          <div class="rec-score">${Math.round(r.recommendationScore)}점</div>
          <button class="btn-add-rec" onclick="addUnit('${r.targetUnit.id}')">+ 필드에 추가</button>
        </div>
      `;
      recListEl.appendChild(item);
    });
  } else {
    recListEl.innerHTML = '<div class="rec-empty">추천할 유닛이 없거나 이미 풀스펙입니다.</div>';
  }

  // 4-1. 21라운드 항법 추천 렌더링
  const navRecListEl = document.getElementById('navRecList');
  if (navRecListEl && data.navigationRecommendations) {
    navRecListEl.innerHTML = '';
    data.navigationRecommendations.slice(0, 3).forEach(nr => {
      const item = document.createElement('div');
      item.className = 'nav-rec-item';
      item.innerHTML = `
        <div class="nav-title-row">
          <span class="nav-name">👑 ${nr.style.name}</span>
          <span class="nav-score">적합도 ${Math.round(nr.matchScore)}%</span>
        </div>
        <div class="nav-rationale">${nr.rationale}</div>
        <div class="nav-tip">💡 팁: ${nr.practicalTip}</div>
      `;
      navRecListEl.appendChild(item);
    });
  }

  // 5. 현재 보유 덱 카드 태그 렌더링
  const currentDeckContainer = document.getElementById('currentDeckContainer');
  const deckCountEl = document.getElementById('deckCount');
  currentDeckContainer.innerHTML = '';
  deckCountEl.textContent = currentUnits.length;

  if (currentUnits.length > 0) {
    currentUnits.forEach(u => {
      const tag = document.createElement('div');
      tag.className = 'deck-tag';
      tag.innerHTML = `<span>${u.name}</span> <span class="remove-icon">×</span>`;
      tag.onclick = () => removeUnit(u.id);
      currentDeckContainer.appendChild(tag);
    });
  } else {
    currentDeckContainer.innerHTML = '<div class="deck-empty">도감에서 유닛을 클릭해 추가하세요.</div>';
  }

  // 6. 오로성 상태 갱신
  updateGoroseiCards(totalArmor, analysis.totalSlow);
}

// 오로성 직관 설명 갱신
function updateGoroseiCards(armor, slow) {
  const descW = document.getElementById('descWarcury');
  const descN = document.getElementById('descNusjuro');

  const afterW = armor + 15;
  const afterN = slow + 15;

  if (armor >= 211) {
    descW.innerHTML = `<span style="color: #94a3b8">현재 ${armor}깎 → <strong>${afterW}깎</strong> (이미 211 풀방깎 달성으로 딜 체감 미미)</span>`;
  } else {
    descW.innerHTML = `<span style="color: #38bdf8">현재 ${armor}깎 → <strong>${afterW}깎</strong> (풀방깎 보완 기여)</span>`;
  }

  if (slow < 102) {
    descN.innerHTML = `<span style="color: #10b981">현재 ${slow}% → <strong>${afterN}%</strong> (★풀이감 102% 즉시 완성!)</span>`;
  } else {
    descN.innerHTML = `<span style="color: #38bdf8">현재 ${slow}% → <strong>${afterN}%</strong> (풀이감 초과 유지)</span>`;
  }
}

// 유닛 도감 피커 렌더링
function renderUnitPicker() {
  const grid = document.getElementById('unitPickerGrid');
  const search = document.getElementById('unitSearch').value.toLowerCase().trim();

  grid.innerHTML = '';

  const filtered = allUnits.filter(u => {
    const matchSearch = u.name.toLowerCase().includes(search) || u.id.toLowerCase().includes(search);
    const matchTier = (activeTier === 'ALL') || (tierName(u.tier) === activeTier || u.tier.toString() === activeTier);
    return matchSearch && matchTier;
  });

  filtered.forEach(u => {
    const card = document.createElement('div');
    card.className = 'unit-card';
    card.onclick = () => addUnit(u.id);

    const statsText = [];
    if (u.synergy.armorReduction > 0) statsText.push(`${u.synergy.armorReduction}깎`);
    if (u.synergy.armorBreakOverlap > 0) statsText.push(`암브${u.synergy.armorBreakOverlap}`);
    if (u.synergy.movementSlow > 0) statsText.push(`이감${u.synergy.movementSlow}%`);
    if (u.synergy.stunValue > 0) statsText.push(`${u.synergy.stunValue}스턴`);
    if (u.synergy.magicArmorReduction > 0) statsText.push(`마깎${u.synergy.magicArmorReduction}%`);

    card.innerHTML = `
      <div class="card-title-row">
        <span class="card-name">${u.name}</span>
        <span class="card-tier">${tierName(u.tier)}</span>
      </div>
      <div class="card-stats-row">
        ${statsText.join(' · ') || '메인 캐리'}
      </div>
    `;
    grid.appendChild(card);
  });
}

function tierName(tier) {
  const map = {
    1: '흔함', 2: '안흔함', 3: '특별함', 4: '희귀함', 5: '히든',
    6: '변화', 7: '전설', 8: '제한', 9: '초월', 10: '불멸',
    11: '영원', 12: '랜덤', 13: '오로성'
  };
  return map[tier] || tier;
}

function filterUnits() {
  renderUnitPicker();
}

function setTierFilter(tier) {
  activeTier = tier;
  document.querySelectorAll('.tab-btn').forEach(btn => btn.classList.remove('active'));
  event.target.classList.add('active');
  renderUnitPicker();
}

// 덱 조작
function addUnit(id) {
  currentDeckIds.push(id);
  evaluateDeck();
}

function removeUnit(id) {
  const idx = currentDeckIds.indexOf(id);
  if (idx !== -1) {
    currentDeckIds.splice(idx, 1);
    evaluateDeck();
  }
}

function toggleUnit(id) {
  const idx = currentDeckIds.indexOf(id);
  if (idx !== -1) {
    currentDeckIds.splice(idx, 1);
  } else {
    currentDeckIds.push(id);
  }
  evaluateDeck();
}

function resetDeck() {
  currentDeckIds = [];
  evaluateDeck();
}

// 프리셋 로드
function loadPreset(type) {
  if (type === 'luffy') {
    currentDeckIds = ['luffy_trans', 'fujitora_leg'];
  } else if (type === 'jinbe_king') {
    currentDeckIds = ['jinbe_trans', 'king_limit', 'dragon_leg', 'vergo_hid'];
  } else if (type === 'question_case') {
    // 211깎 · 102이감 유저 질문 스펙 완벽 재현
    currentDeckIds = [
      'luffy_trans',     // 10깎 + 25암브
      'zoro_trans',      // 35깎
      'king_limit',      // 30깎 + 15이감
      'shinobu_leg',     // 30깎 + 15이감
      'vergo_hid',       // 35깎 + 20암브
      'ace_chg',         // 30깎
      'fujitora_leg',    // 12깎 + 30이감 + 1스턴
      'smoker_leg',      // 10깎 + 25이감
      'bonkure_hid',     // 11깎 + 0.5스턴
      'perona_hid'       // 35이감
    ];
  } else if (type === 'shirahoshi') {
    currentDeckIds = ['shirahoshi_trans', 'bartolomeo_leg', 'reiju_leg'];
  }
  evaluateDeck();
}
