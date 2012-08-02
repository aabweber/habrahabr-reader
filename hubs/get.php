<?php
	set_time_limit(0);
	//$c = file_get_contents('http://habrahabr.ru/hubs/');
	$c = '<li>

					

						<a href="/hubs/api">API</a>

						<span class="counter">(8)</span>

					

					

					

				</li>

			

				<li>

					

						<a href="/hubs/administration">Администрирование</a>

						<span class="counter">(6)</span>

					

					

					

				</li>

			

				<li>

					

						<a href="/hubs/databases">Базы данных</a>

						<span class="counter">(12)</span>

					

					

					

				</li>

			

				<li>

					

						<a href="/hubs/security">Безопасность</a>

						<span class="counter">(4)</span>

					

					

					

				</li>

			

				<li>

					

						<a href="/hubs/design-and-media">Дизайн, графика, звук</a>

						<span class="counter">(13)</span>

					

					

					

				</li>

			

				<li>

					

						<a href="/hubs/hardware">Железо и гаджеты </a>

						<span class="counter">(33)</span>

					

					

					

				</li>

			

				<li>

					

						<a href="/hubs/companies-and-services">Компании и сервисы </a>

						<span class="counter">(35)</span>

					

					

					

				</li>

			

				<li>

					

						<a href="/hubs/management">Менеджмент</a>

						<span class="counter">(12)</span>

					

					

					<span class="new">new</span>

				</li>

			

				<li>

					

						<a href="/hubs/programming">Программирование</a>

						<span class="counter">(66)</span>

					

					

					<span class="new">new</span>

				</li>

			

				<li>

					

						<a href="/hubs/software">Программное обеспечение</a>

						<span class="counter">(24)</span>

					

					

					

				</li>

			

				<li>

					

						<a href="/hubs/telecommunications">Телекоммуникации </a>

						<span class="counter">(26)</span>

					

					

					

				</li>

			

				<li>

					

						<a href="/hubs/fw-and-cms">Фреймворки и CMS</a>

						<span class="counter">(24)</span>

					

					

					

				</li>

			

				<li>

					

						<a href="/hubs/frontend">Фронтэнд</a>

						<span class="counter">(13)</span>

					

					

					

				</li>

			

				<li>

					

						<a href="/hubs/others">Разное </a>

						<span class="counter">(53)</span>

					

					

					

				</li>
';
	preg_match_all('/<a href="\/hubs\/(.+?)">(.+?)<\/a>/si', $c, $ms);
	//print_r($ms);
	echo "List<Hub> hubs = new List<Hub>{\n";
	foreach($ms[1] as $key => $enname){
		$name = $ms[2][$key];
		echo "	new Hub(\"$enname\", \"$name\", \n";
		echo "		new List<Hub>{\n";
		
		$c = file_get_contents('http://habrahabr.ru/hubs/'.$enname);
		preg_match_all('/<a href="http:\/\/habrahabr.ru\/hub\/(.+?)">(.+?)<\/a>/si', $c, $ms2);
		foreach($ms2[1] as $key2 => $enname2){
			$name2 = $ms2[2][$key2];
			echo "			new Hub(\"$enname2\", \"$name2\"), \n";
			//print_r($ms2);exit;
		}
		echo "		}\n";
		echo "	), \n";
	}
	echo "};";
?>




































