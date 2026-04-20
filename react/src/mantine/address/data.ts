/**
 * Address data: all ~195 countries with ISO-3166-1 alpha-2 codes.
 * Pakistan has full provinces/territories + major cities.
 * Other selected countries have states + cities.
 * Remaining countries have empty states arrays.
 */

export interface CityData {
    name: string;
}

export interface StateData {
    code: string;
    name: string;
    cities: CityData[];
}

export interface CountryData {
    code: string; // ISO 3166-1 alpha-2
    name: string;
    states: StateData[];
}

export const COUNTRIES: CountryData[] = [
    // Africa
    { code: 'DZ', name: 'Algeria', states: [] },
    { code: 'AO', name: 'Angola', states: [] },
    { code: 'BJ', name: 'Benin', states: [] },
    { code: 'BW', name: 'Botswana', states: [] },
    { code: 'BF', name: 'Burkina Faso', states: [] },
    { code: 'BI', name: 'Burundi', states: [] },
    { code: 'CV', name: 'Cape Verde', states: [] },
    { code: 'CM', name: 'Cameroon', states: [] },
    { code: 'CF', name: 'Central African Republic', states: [] },
    { code: 'TD', name: 'Chad', states: [] },
    { code: 'KM', name: 'Comoros', states: [] },
    { code: 'CG', name: 'Congo', states: [] },
    { code: 'CD', name: 'Congo (Democratic Republic)', states: [] },
    { code: 'DJ', name: 'Djibouti', states: [] },
    { code: 'EG', name: 'Egypt', states: [] },
    { code: 'GQ', name: 'Equatorial Guinea', states: [] },
    { code: 'ER', name: 'Eritrea', states: [] },
    { code: 'SZ', name: 'Eswatini', states: [] },
    { code: 'ET', name: 'Ethiopia', states: [] },
    { code: 'GA', name: 'Gabon', states: [] },
    { code: 'GM', name: 'Gambia', states: [] },
    { code: 'GH', name: 'Ghana', states: [] },
    { code: 'GN', name: 'Guinea', states: [] },
    { code: 'GW', name: 'Guinea-Bissau', states: [] },
    { code: 'CI', name: "Cote d'Ivoire", states: [] },
    { code: 'KE', name: 'Kenya', states: [] },
    { code: 'LS', name: 'Lesotho', states: [] },
    { code: 'LR', name: 'Liberia', states: [] },
    { code: 'LY', name: 'Libya', states: [] },
    { code: 'MG', name: 'Madagascar', states: [] },
    { code: 'MW', name: 'Malawi', states: [] },
    { code: 'ML', name: 'Mali', states: [] },
    { code: 'MR', name: 'Mauritania', states: [] },
    { code: 'MU', name: 'Mauritius', states: [] },
    { code: 'MA', name: 'Morocco', states: [] },
    { code: 'MZ', name: 'Mozambique', states: [] },
    { code: 'NA', name: 'Namibia', states: [] },
    { code: 'NE', name: 'Niger', states: [] },
    { code: 'NG', name: 'Nigeria', states: [] },
    { code: 'RW', name: 'Rwanda', states: [] },
    { code: 'ST', name: 'Sao Tome and Principe', states: [] },
    { code: 'SN', name: 'Senegal', states: [] },
    { code: 'SC', name: 'Seychelles', states: [] },
    { code: 'SL', name: 'Sierra Leone', states: [] },
    { code: 'SO', name: 'Somalia', states: [] },
    {
        code: 'ZA', name: 'South Africa', states: [
            { code: 'GP', name: 'Gauteng', cities: [{ name: 'Johannesburg' }, { name: 'Pretoria' }, { name: 'Soweto' }] },
            { code: 'WC', name: 'Western Cape', cities: [{ name: 'Cape Town' }, { name: 'Stellenbosch' }] },
            { code: 'KZN', name: 'KwaZulu-Natal', cities: [{ name: 'Durban' }, { name: 'Pietermaritzburg' }] },
        ]
    },
    { code: 'SS', name: 'South Sudan', states: [] },
    { code: 'SD', name: 'Sudan', states: [] },
    { code: 'TZ', name: 'Tanzania', states: [] },
    { code: 'TG', name: 'Togo', states: [] },
    { code: 'TN', name: 'Tunisia', states: [] },
    { code: 'UG', name: 'Uganda', states: [] },
    { code: 'ZM', name: 'Zambia', states: [] },
    { code: 'ZW', name: 'Zimbabwe', states: [] },

    // Americas
    { code: 'AG', name: 'Antigua and Barbuda', states: [] },
    {
        code: 'AR', name: 'Argentina', states: [
            { code: 'BA', name: 'Buenos Aires', cities: [{ name: 'Buenos Aires' }, { name: 'La Plata' }, { name: 'Mar del Plata' }] },
            { code: 'CBA', name: 'Cordoba', cities: [{ name: 'Cordoba' }, { name: 'Villa Carlos Paz' }] },
            { code: 'SF', name: 'Santa Fe', cities: [{ name: 'Rosario' }, { name: 'Santa Fe' }] },
        ]
    },
    { code: 'BS', name: 'Bahamas', states: [] },
    { code: 'BB', name: 'Barbados', states: [] },
    { code: 'BZ', name: 'Belize', states: [] },
    {
        code: 'BO', name: 'Bolivia', states: [
            { code: 'LP', name: 'La Paz', cities: [{ name: 'La Paz' }, { name: 'El Alto' }] },
            { code: 'SCZ', name: 'Santa Cruz', cities: [{ name: 'Santa Cruz de la Sierra' }] },
        ]
    },
    {
        code: 'BR', name: 'Brazil', states: [
            { code: 'SP', name: 'Sao Paulo', cities: [{ name: 'Sao Paulo' }, { name: 'Campinas' }, { name: 'Santos' }] },
            { code: 'RJ', name: 'Rio de Janeiro', cities: [{ name: 'Rio de Janeiro' }, { name: 'Niteroi' }] },
            { code: 'MG', name: 'Minas Gerais', cities: [{ name: 'Belo Horizonte' }, { name: 'Uberlandia' }] },
            { code: 'RS', name: 'Rio Grande do Sul', cities: [{ name: 'Porto Alegre' }, { name: 'Caxias do Sul' }] },
            { code: 'BA', name: 'Bahia', cities: [{ name: 'Salvador' }, { name: 'Feira de Santana' }] },
        ]
    },
    {
        code: 'CA', name: 'Canada', states: [
            { code: 'ON', name: 'Ontario', cities: [{ name: 'Toronto' }, { name: 'Ottawa' }, { name: 'Mississauga' }, { name: 'Hamilton' }, { name: 'Brampton' }, { name: 'London' }] },
            { code: 'QC', name: 'Quebec', cities: [{ name: 'Montreal' }, { name: 'Quebec City' }, { name: 'Laval' }, { name: 'Gatineau' }, { name: 'Longueuil' }] },
            { code: 'BC', name: 'British Columbia', cities: [{ name: 'Vancouver' }, { name: 'Victoria' }, { name: 'Surrey' }, { name: 'Burnaby' }, { name: 'Kelowna' }] },
            { code: 'AB', name: 'Alberta', cities: [{ name: 'Calgary' }, { name: 'Edmonton' }, { name: 'Red Deer' }, { name: 'Lethbridge' }] },
            { code: 'MB', name: 'Manitoba', cities: [{ name: 'Winnipeg' }, { name: 'Brandon' }] },
            { code: 'SK', name: 'Saskatchewan', cities: [{ name: 'Saskatoon' }, { name: 'Regina' }] },
            { code: 'NS', name: 'Nova Scotia', cities: [{ name: 'Halifax' }] },
            { code: 'NB', name: 'New Brunswick', cities: [{ name: 'Moncton' }, { name: 'Fredericton' }] },
            { code: 'NL', name: 'Newfoundland and Labrador', cities: [{ name: "St. John's" }] },
            { code: 'PE', name: 'Prince Edward Island', cities: [{ name: 'Charlottetown' }] },
        ]
    },
    {
        code: 'CL', name: 'Chile', states: [
            { code: 'RM', name: 'Region Metropolitana', cities: [{ name: 'Santiago' }, { name: 'Maipu' }] },
            { code: 'VA', name: 'Valparaiso', cities: [{ name: 'Valparaiso' }, { name: 'Vina del Mar' }] },
        ]
    },
    {
        code: 'CO', name: 'Colombia', states: [
            { code: 'DC', name: 'Bogota D.C.', cities: [{ name: 'Bogota' }] },
            { code: 'ANT', name: 'Antioquia', cities: [{ name: 'Medellin' }, { name: 'Bello' }] },
            { code: 'VAC', name: 'Valle del Cauca', cities: [{ name: 'Cali' }, { name: 'Buenaventura' }] },
        ]
    },
    { code: 'CR', name: 'Costa Rica', states: [] },
    { code: 'CU', name: 'Cuba', states: [] },
    { code: 'DM', name: 'Dominica', states: [] },
    { code: 'DO', name: 'Dominican Republic', states: [] },
    { code: 'EC', name: 'Ecuador', states: [] },
    { code: 'SV', name: 'El Salvador', states: [] },
    { code: 'GD', name: 'Grenada', states: [] },
    { code: 'GT', name: 'Guatemala', states: [] },
    { code: 'GY', name: 'Guyana', states: [] },
    { code: 'HT', name: 'Haiti', states: [] },
    { code: 'HN', name: 'Honduras', states: [] },
    { code: 'JM', name: 'Jamaica', states: [] },
    {
        code: 'MX', name: 'Mexico', states: [
            { code: 'CMX', name: 'Mexico City', cities: [{ name: 'Mexico City' }] },
            { code: 'JAL', name: 'Jalisco', cities: [{ name: 'Guadalajara' }, { name: 'Zapopan' }] },
            { code: 'NLE', name: 'Nuevo Leon', cities: [{ name: 'Monterrey' }, { name: 'San Nicolas' }] },
            { code: 'PUE', name: 'Puebla', cities: [{ name: 'Puebla' }, { name: 'Tehuacan' }] },
            { code: 'YUC', name: 'Yucatan', cities: [{ name: 'Merida' }] },
        ]
    },
    { code: 'NI', name: 'Nicaragua', states: [] },
    { code: 'PA', name: 'Panama', states: [] },
    { code: 'PY', name: 'Paraguay', states: [] },
    { code: 'PE', name: 'Peru', states: [] },
    { code: 'KN', name: 'Saint Kitts and Nevis', states: [] },
    { code: 'LC', name: 'Saint Lucia', states: [] },
    { code: 'VC', name: 'Saint Vincent and the Grenadines', states: [] },
    { code: 'SR', name: 'Suriname', states: [] },
    { code: 'TT', name: 'Trinidad and Tobago', states: [] },
    {
        code: 'US', name: 'United States', states: [
            { code: 'CA', name: 'California', cities: [{ name: 'Los Angeles' }, { name: 'San Francisco' }, { name: 'San Diego' }, { name: 'Sacramento' }, { name: 'San Jose' }, { name: 'Oakland' }, { name: 'Fresno' }, { name: 'Long Beach' }] },
            { code: 'NY', name: 'New York', cities: [{ name: 'New York City' }, { name: 'Buffalo' }, { name: 'Rochester' }, { name: 'Yonkers' }, { name: 'Syracuse' }, { name: 'Albany' }] },
            { code: 'TX', name: 'Texas', cities: [{ name: 'Houston' }, { name: 'Dallas' }, { name: 'Austin' }, { name: 'San Antonio' }, { name: 'Fort Worth' }, { name: 'El Paso' }] },
            { code: 'FL', name: 'Florida', cities: [{ name: 'Miami' }, { name: 'Orlando' }, { name: 'Tampa' }, { name: 'Jacksonville' }, { name: 'Tallahassee' }, { name: 'Fort Lauderdale' }] },
            { code: 'WA', name: 'Washington', cities: [{ name: 'Seattle' }, { name: 'Spokane' }, { name: 'Tacoma' }, { name: 'Bellevue' }, { name: 'Redmond' }] },
            { code: 'IL', name: 'Illinois', cities: [{ name: 'Chicago' }, { name: 'Aurora' }, { name: 'Naperville' }, { name: 'Rockford' }, { name: 'Springfield' }] },
            { code: 'CO', name: 'Colorado', cities: [{ name: 'Denver' }, { name: 'Colorado Springs' }, { name: 'Aurora' }, { name: 'Fort Collins' }, { name: 'Boulder' }] },
            { code: 'GA', name: 'Georgia', cities: [{ name: 'Atlanta' }, { name: 'Columbus' }, { name: 'Augusta' }, { name: 'Savannah' }, { name: 'Athens' }] },
            { code: 'AZ', name: 'Arizona', cities: [{ name: 'Phoenix' }, { name: 'Tucson' }, { name: 'Mesa' }, { name: 'Scottsdale' }] },
            { code: 'PA', name: 'Pennsylvania', cities: [{ name: 'Philadelphia' }, { name: 'Pittsburgh' }, { name: 'Allentown' }] },
            { code: 'OH', name: 'Ohio', cities: [{ name: 'Columbus' }, { name: 'Cleveland' }, { name: 'Cincinnati' }] },
            { code: 'MI', name: 'Michigan', cities: [{ name: 'Detroit' }, { name: 'Grand Rapids' }, { name: 'Warren' }] },
            { code: 'NC', name: 'North Carolina', cities: [{ name: 'Charlotte' }, { name: 'Raleigh' }, { name: 'Greensboro' }] },
            { code: 'NV', name: 'Nevada', cities: [{ name: 'Las Vegas' }, { name: 'Henderson' }, { name: 'Reno' }] },
            { code: 'MN', name: 'Minnesota', cities: [{ name: 'Minneapolis' }, { name: 'Saint Paul' }] },
            { code: 'OR', name: 'Oregon', cities: [{ name: 'Portland' }, { name: 'Salem' }, { name: 'Eugene' }] },
            { code: 'MA', name: 'Massachusetts', cities: [{ name: 'Boston' }, { name: 'Worcester' }, { name: 'Springfield' }] },
            { code: 'VA', name: 'Virginia', cities: [{ name: 'Virginia Beach' }, { name: 'Norfolk' }, { name: 'Arlington' }] },
            { code: 'TN', name: 'Tennessee', cities: [{ name: 'Nashville' }, { name: 'Memphis' }, { name: 'Knoxville' }] },
            { code: 'MO', name: 'Missouri', cities: [{ name: 'Kansas City' }, { name: 'St. Louis' }, { name: 'Springfield' }] },
        ]
    },
    { code: 'UY', name: 'Uruguay', states: [] },
    { code: 'VE', name: 'Venezuela', states: [] },

    // Asia
    { code: 'AF', name: 'Afghanistan', states: [] },
    { code: 'AM', name: 'Armenia', states: [] },
    { code: 'AZ', name: 'Azerbaijan', states: [] },
    { code: 'BH', name: 'Bahrain', states: [] },
    {
        code: 'BD', name: 'Bangladesh', states: [
            { code: 'DHA', name: 'Dhaka', cities: [{ name: 'Dhaka' }, { name: 'Narayanganj' }, { name: 'Gazipur' }] },
            { code: 'CHG', name: 'Chittagong', cities: [{ name: 'Chittagong' }, { name: "Cox's Bazar" }] },
        ]
    },
    { code: 'BT', name: 'Bhutan', states: [] },
    { code: 'BN', name: 'Brunei', states: [] },
    { code: 'KH', name: 'Cambodia', states: [] },
    {
        code: 'CN', name: 'China', states: [
            { code: 'BJ', name: 'Beijing', cities: [{ name: 'Beijing' }] },
            { code: 'SH', name: 'Shanghai', cities: [{ name: 'Shanghai' }] },
            { code: 'GD', name: 'Guangdong', cities: [{ name: 'Guangzhou' }, { name: 'Shenzhen' }, { name: 'Dongguan' }] },
            { code: 'ZJ', name: 'Zhejiang', cities: [{ name: 'Hangzhou' }, { name: 'Ningbo' }] },
            { code: 'JS', name: 'Jiangsu', cities: [{ name: 'Nanjing' }, { name: 'Suzhou' }, { name: 'Wuxi' }] },
            { code: 'SC', name: 'Sichuan', cities: [{ name: 'Chengdu' }, { name: 'Mianyang' }] },
            { code: 'HB', name: 'Hubei', cities: [{ name: 'Wuhan' }, { name: 'Yichang' }] },
            { code: 'SNX', name: 'Shaanxi', cities: [{ name: "Xi'an" }, { name: 'Xianyang' }] },
        ]
    },
    { code: 'CY', name: 'Cyprus', states: [] },
    { code: 'GE', name: 'Georgia', states: [] },
    {
        code: 'IN', name: 'India', states: [
            { code: 'MH', name: 'Maharashtra', cities: [{ name: 'Mumbai' }, { name: 'Pune' }, { name: 'Nagpur' }, { name: 'Nashik' }, { name: 'Aurangabad' }, { name: 'Thane' }] },
            { code: 'DL', name: 'Delhi', cities: [{ name: 'New Delhi' }, { name: 'Delhi' }, { name: 'Noida' }, { name: 'Gurgaon' }, { name: 'Faridabad' }] },
            { code: 'KA', name: 'Karnataka', cities: [{ name: 'Bengaluru' }, { name: 'Mysuru' }, { name: 'Hubli' }, { name: 'Mangaluru' }, { name: 'Belagavi' }] },
            { code: 'TN', name: 'Tamil Nadu', cities: [{ name: 'Chennai' }, { name: 'Coimbatore' }, { name: 'Madurai' }, { name: 'Tiruchirappalli' }, { name: 'Salem' }] },
            { code: 'UP', name: 'Uttar Pradesh', cities: [{ name: 'Lucknow' }, { name: 'Kanpur' }, { name: 'Agra' }, { name: 'Varanasi' }, { name: 'Meerut' }, { name: 'Prayagraj' }] },
            { code: 'WB', name: 'West Bengal', cities: [{ name: 'Kolkata' }, { name: 'Howrah' }, { name: 'Durgapur' }] },
            { code: 'GJ', name: 'Gujarat', cities: [{ name: 'Ahmedabad' }, { name: 'Surat' }, { name: 'Vadodara' }, { name: 'Rajkot' }] },
            { code: 'RJ', name: 'Rajasthan', cities: [{ name: 'Jaipur' }, { name: 'Jodhpur' }, { name: 'Udaipur' }, { name: 'Kota' }] },
            { code: 'TS', name: 'Telangana', cities: [{ name: 'Hyderabad' }, { name: 'Warangal' }, { name: 'Nizamabad' }] },
            { code: 'AP', name: 'Andhra Pradesh', cities: [{ name: 'Visakhapatnam' }, { name: 'Vijayawada' }, { name: 'Guntur' }] },
        ]
    },
    {
        code: 'ID', name: 'Indonesia', states: [
            { code: 'JK', name: 'Jakarta', cities: [{ name: 'Jakarta' }] },
            { code: 'JB', name: 'West Java', cities: [{ name: 'Bandung' }, { name: 'Bekasi' }, { name: 'Bogor' }] },
            { code: 'JT', name: 'Central Java', cities: [{ name: 'Semarang' }, { name: 'Solo' }] },
            { code: 'JI', name: 'East Java', cities: [{ name: 'Surabaya' }, { name: 'Malang' }] },
        ]
    },
    { code: 'IR', name: 'Iran', states: [] },
    { code: 'IQ', name: 'Iraq', states: [] },
    { code: 'IL', name: 'Israel', states: [] },
    {
        code: 'JP', name: 'Japan', states: [
            { code: 'TK', name: 'Tokyo', cities: [{ name: 'Tokyo' }, { name: 'Shinjuku' }, { name: 'Shibuya' }] },
            { code: 'OS', name: 'Osaka', cities: [{ name: 'Osaka' }, { name: 'Sakai' }] },
            { code: 'KN', name: 'Kanagawa', cities: [{ name: 'Yokohama' }, { name: 'Kawasaki' }] },
            { code: 'AI', name: 'Aichi', cities: [{ name: 'Nagoya' }] },
            { code: 'HK', name: 'Hokkaido', cities: [{ name: 'Sapporo' }, { name: 'Asahikawa' }] },
            { code: 'FK', name: 'Fukuoka', cities: [{ name: 'Fukuoka' }, { name: 'Kitakyushu' }] },
        ]
    },
    { code: 'JO', name: 'Jordan', states: [] },
    { code: 'KZ', name: 'Kazakhstan', states: [] },
    { code: 'KW', name: 'Kuwait', states: [] },
    { code: 'KG', name: 'Kyrgyzstan', states: [] },
    { code: 'LA', name: 'Laos', states: [] },
    { code: 'LB', name: 'Lebanon', states: [] },
    {
        code: 'MY', name: 'Malaysia', states: [
            { code: 'KUL', name: 'Kuala Lumpur', cities: [{ name: 'Kuala Lumpur' }] },
            { code: 'SLG', name: 'Selangor', cities: [{ name: 'Shah Alam' }, { name: 'Petaling Jaya' }, { name: 'Klang' }] },
            { code: 'PNG', name: 'Penang', cities: [{ name: 'George Town' }, { name: 'Butterworth' }] },
            { code: 'JHR', name: 'Johor', cities: [{ name: 'Johor Bahru' }, { name: 'Batu Pahat' }] },
        ]
    },
    { code: 'MV', name: 'Maldives', states: [] },
    { code: 'MN', name: 'Mongolia', states: [] },
    { code: 'MM', name: 'Myanmar', states: [] },
    { code: 'NP', name: 'Nepal', states: [] },
    { code: 'KP', name: 'North Korea', states: [] },
    { code: 'OM', name: 'Oman', states: [] },
    {
        code: 'PK', name: 'Pakistan', states: [
            {
                code: 'PB', name: 'Punjab', cities: [
                    { name: 'Lahore' }, { name: 'Faisalabad' }, { name: 'Rawalpindi' }, { name: 'Gujranwala' },
                    { name: 'Multan' }, { name: 'Sialkot' }, { name: 'Bahawalpur' }, { name: 'Sargodha' },
                    { name: 'Sheikhupura' }, { name: 'Gujrat' }, { name: 'Jhang' }, { name: 'Rahim Yar Khan' },
                    { name: 'Kasur' }, { name: 'Okara' }, { name: 'Chiniot' }, { name: 'Sahiwal' },
                    { name: 'Mandi Bahauddin' }, { name: 'Jhelum' }, { name: 'Wah Cantonment' }, { name: 'Attock' },
                    { name: 'Chakwal' }, { name: 'Hafizabad' }, { name: 'Narowal' }, { name: 'Pakpattan' },
                    { name: 'Vehari' }, { name: 'Khushab' }, { name: 'Bhakkar' }, { name: 'Muzaffargarh' },
                    { name: 'Lodhran' }, { name: 'Khanewal' }, { name: 'Toba Tek Singh' }, { name: 'Nankana Sahib' },
                    { name: 'Murree' }, { name: 'Taxila' }, { name: 'Dera Ghazi Khan' }, { name: 'Layyah' },
                    { name: 'Rajanpur' },
                ]
            },
            {
                code: 'SD', name: 'Sindh', cities: [
                    { name: 'Karachi' }, { name: 'Hyderabad' }, { name: 'Sukkur' }, { name: 'Larkana' },
                    { name: 'Nawabshah' }, { name: 'Thatta' }, { name: 'Mirpur Khas' }, { name: 'Jacobabad' },
                    { name: 'Shikarpur' }, { name: 'Khairpur' }, { name: 'Dadu' }, { name: 'Sanghar' },
                    { name: 'Badin' }, { name: 'Matiari' }, { name: 'Jamshoro' }, { name: 'Kashmore' },
                    { name: 'Ghotki' }, { name: 'Naushahro Feroze' }, { name: 'Umerkot' }, { name: 'Tando Allahyar' },
                    { name: 'Tando Muhammad Khan' }, { name: 'Qambar Shahdadkot' },
                ]
            },
            {
                code: 'KP', name: 'Khyber Pakhtunkhwa', cities: [
                    { name: 'Peshawar' }, { name: 'Mardan' }, { name: 'Mingora' }, { name: 'Kohat' },
                    { name: 'Abbottabad' }, { name: 'Mansehra' }, { name: 'Dera Ismail Khan' }, { name: 'Bannu' },
                    { name: 'Nowshera' }, { name: 'Charsadda' }, { name: 'Swabi' }, { name: 'Haripur' },
                    { name: 'Malakand' }, { name: 'Chitral' }, { name: 'Dir' }, { name: 'Hangu' },
                    { name: 'Karak' }, { name: 'Lakki Marwat' }, { name: 'Buner' }, { name: 'Shangla' },
                    { name: 'Battagram' }, { name: 'Tank' },
                ]
            },
            {
                code: 'BA', name: 'Balochistan', cities: [
                    { name: 'Quetta' }, { name: 'Turbat' }, { name: 'Khuzdar' }, { name: 'Hub' },
                    { name: 'Gwadar' }, { name: 'Chaman' }, { name: 'Zhob' }, { name: 'Loralai' },
                    { name: 'Sibi' }, { name: 'Kharan' }, { name: 'Mastung' }, { name: 'Panjgur' },
                    { name: 'Awaran' }, { name: 'Lasbela' }, { name: 'Ziarat' }, { name: 'Pishin' },
                    { name: 'Nushki' }, { name: 'Chaghi' }, { name: 'Killa Abdullah' }, { name: 'Killa Saifullah' },
                    { name: 'Kalat' }, { name: 'Dera Bugti' }, { name: 'Kohlu' }, { name: 'Barkhan' },
                ]
            },
            {
                code: 'ICT', name: 'Islamabad Capital Territory', cities: [
                    { name: 'Islamabad' },
                ]
            },
            {
                code: 'AJK', name: 'Azad Jammu and Kashmir', cities: [
                    { name: 'Muzaffarabad' }, { name: 'Mirpur' }, { name: 'Rawalakot' }, { name: 'Kotli' },
                    { name: 'Bagh' }, { name: 'Bhimber' }, { name: 'Haveli' }, { name: 'Poonch' },
                    { name: 'Sudhnuti' }, { name: 'Neelum' },
                ]
            },
            {
                code: 'GB', name: 'Gilgit-Baltistan', cities: [
                    { name: 'Gilgit' }, { name: 'Skardu' }, { name: 'Hunza' }, { name: 'Ghizer' },
                    { name: 'Astore' }, { name: 'Diamer' }, { name: 'Ghanche' }, { name: 'Shigar' },
                    { name: 'Nagar' },
                ]
            },
        ]
    },
    {
        code: 'PH', name: 'Philippines', states: [
            { code: 'NCR', name: 'Metro Manila', cities: [{ name: 'Manila' }, { name: 'Quezon City' }, { name: 'Caloocan' }, { name: 'Pasig' }] },
            { code: 'CEB', name: 'Cebu', cities: [{ name: 'Cebu City' }, { name: 'Mandaue' }] },
            { code: 'DAV', name: 'Davao', cities: [{ name: 'Davao City' }] },
        ]
    },
    { code: 'QA', name: 'Qatar', states: [] },
    {
        code: 'SA', name: 'Saudi Arabia', states: [
            { code: 'RYD', name: 'Riyadh', cities: [{ name: 'Riyadh' }, { name: 'Al-Kharj' }, { name: 'Dawadmi' }] },
            { code: 'MKH', name: 'Makkah', cities: [{ name: 'Mecca' }, { name: 'Jeddah' }, { name: 'Taif' }] },
            { code: 'MED', name: 'Madinah', cities: [{ name: 'Medina' }, { name: 'Yanbu' }, { name: 'Al Ula' }] },
            { code: 'EAS', name: 'Eastern Province', cities: [{ name: 'Dammam' }, { name: 'Al Khobar' }, { name: 'Dhahran' }, { name: 'Jubail' }, { name: 'Hafar Al-Batin' }] },
            { code: 'ASI', name: 'Asir', cities: [{ name: 'Abha' }, { name: 'Khamis Mushait' }] },
            { code: 'HAI', name: "Ha'il", cities: [{ name: "Ha'il" }] },
            { code: 'TAB', name: 'Tabuk', cities: [{ name: 'Tabuk' }] },
        ]
    },
    {
        code: 'SG', name: 'Singapore', states: [
            { code: 'SG', name: 'Singapore', cities: [{ name: 'Singapore' }] },
        ]
    },
    {
        code: 'KR', name: 'South Korea', states: [
            { code: 'SE', name: 'Seoul', cities: [{ name: 'Seoul' }] },
            { code: 'BS', name: 'Busan', cities: [{ name: 'Busan' }] },
            { code: 'IC', name: 'Incheon', cities: [{ name: 'Incheon' }] },
            { code: 'GG', name: 'Gyeonggi', cities: [{ name: 'Suwon' }, { name: 'Seongnam' }, { name: 'Goyang' }] },
        ]
    },
    { code: 'LK', name: 'Sri Lanka', states: [] },
    { code: 'SY', name: 'Syria', states: [] },
    { code: 'TW', name: 'Taiwan', states: [] },
    { code: 'TJ', name: 'Tajikistan', states: [] },
    {
        code: 'TH', name: 'Thailand', states: [
            { code: 'BKK', name: 'Bangkok', cities: [{ name: 'Bangkok' }] },
            { code: 'CM', name: 'Chiang Mai', cities: [{ name: 'Chiang Mai' }] },
            { code: 'PKT', name: 'Phuket', cities: [{ name: 'Phuket' }] },
        ]
    },
    { code: 'TL', name: 'Timor-Leste', states: [] },
    {
        code: 'TR', name: 'Turkey', states: [
            { code: 'IST', name: 'Istanbul', cities: [{ name: 'Istanbul' }] },
            { code: 'ANK', name: 'Ankara', cities: [{ name: 'Ankara' }] },
            { code: 'IZM', name: 'Izmir', cities: [{ name: 'Izmir' }] },
            { code: 'ANT', name: 'Antalya', cities: [{ name: 'Antalya' }, { name: 'Alanya' }] },
            { code: 'BRS', name: 'Bursa', cities: [{ name: 'Bursa' }] },
        ]
    },
    { code: 'TM', name: 'Turkmenistan', states: [] },
    {
        code: 'AE', name: 'United Arab Emirates', states: [
            { code: 'DXB', name: 'Dubai', cities: [{ name: 'Dubai' }, { name: 'Jumeirah' }, { name: 'Deira' }, { name: 'Bur Dubai' }] },
            { code: 'AUH', name: 'Abu Dhabi', cities: [{ name: 'Abu Dhabi' }, { name: 'Al Ain' }, { name: 'Ruwais' }] },
            { code: 'SHJ', name: 'Sharjah', cities: [{ name: 'Sharjah' }, { name: 'Khor Fakkan' }] },
            { code: 'AJM', name: 'Ajman', cities: [{ name: 'Ajman' }] },
            { code: 'FUJ', name: 'Fujairah', cities: [{ name: 'Fujairah' }] },
            { code: 'RAK', name: 'Ras Al Khaimah', cities: [{ name: 'Ras Al Khaimah' }] },
            { code: 'UAQ', name: 'Umm Al Quwain', cities: [{ name: 'Umm Al Quwain' }] },
        ]
    },
    { code: 'UZ', name: 'Uzbekistan', states: [] },
    {
        code: 'VN', name: 'Vietnam', states: [
            { code: 'HN', name: 'Hanoi', cities: [{ name: 'Hanoi' }] },
            { code: 'SGN', name: 'Ho Chi Minh City', cities: [{ name: 'Ho Chi Minh City' }] },
            { code: 'DN', name: 'Da Nang', cities: [{ name: 'Da Nang' }] },
        ]
    },
    { code: 'YE', name: 'Yemen', states: [] },

    // Europe
    { code: 'AL', name: 'Albania', states: [] },
    { code: 'AD', name: 'Andorra', states: [] },
    {
        code: 'AT', name: 'Austria', states: [
            { code: 'W', name: 'Vienna', cities: [{ name: 'Vienna' }] },
            { code: 'ST', name: 'Styria', cities: [{ name: 'Graz' }] },
            { code: 'OO', name: 'Upper Austria', cities: [{ name: 'Linz' }] },
        ]
    },
    { code: 'BY', name: 'Belarus', states: [] },
    {
        code: 'BE', name: 'Belgium', states: [
            { code: 'VAN', name: 'Flanders', cities: [{ name: 'Antwerp' }, { name: 'Ghent' }, { name: 'Bruges' }] },
            { code: 'WAL', name: 'Wallonia', cities: [{ name: 'Liege' }, { name: 'Namur' }] },
            { code: 'BRU', name: 'Brussels', cities: [{ name: 'Brussels' }] },
        ]
    },
    { code: 'BA', name: 'Bosnia and Herzegovina', states: [] },
    { code: 'BG', name: 'Bulgaria', states: [] },
    { code: 'HR', name: 'Croatia', states: [] },
    { code: 'CZ', name: 'Czech Republic', states: [] },
    {
        code: 'DK', name: 'Denmark', states: [
            { code: 'CPH', name: 'Capital Region', cities: [{ name: 'Copenhagen' }, { name: 'Frederiksberg' }] },
            { code: 'MJU', name: 'Central Jutland', cities: [{ name: 'Aarhus' }, { name: 'Viborg' }] },
        ]
    },
    { code: 'EE', name: 'Estonia', states: [] },
    {
        code: 'FI', name: 'Finland', states: [
            { code: 'U', name: 'Uusimaa', cities: [{ name: 'Helsinki' }, { name: 'Espoo' }, { name: 'Vantaa' }] },
            { code: 'PI', name: 'Pirkanmaa', cities: [{ name: 'Tampere' }] },
        ]
    },
    {
        code: 'FR', name: 'France', states: [
            { code: 'IDF', name: 'Ile-de-France', cities: [{ name: 'Paris' }, { name: 'Boulogne-Billancourt' }, { name: 'Saint-Denis' }, { name: 'Versailles' }, { name: 'Nanterre' }] },
            { code: 'ARA', name: 'Auvergne-Rhone-Alpes', cities: [{ name: 'Lyon' }, { name: 'Grenoble' }, { name: 'Clermont-Ferrand' }, { name: 'Annecy' }] },
            { code: 'OCC', name: 'Occitanie', cities: [{ name: 'Toulouse' }, { name: 'Montpellier' }, { name: 'Nimes' }, { name: 'Perpignan' }] },
            { code: 'PAC', name: 'Provence-Alpes-Cote Azur', cities: [{ name: 'Marseille' }, { name: 'Nice' }, { name: 'Toulon' }, { name: 'Aix-en-Provence' }] },
            { code: 'GES', name: 'Grand Est', cities: [{ name: 'Strasbourg' }, { name: 'Reims' }, { name: 'Metz' }] },
            { code: 'NAQ', name: 'Nouvelle-Aquitaine', cities: [{ name: 'Bordeaux' }, { name: 'Limoges' }] },
            { code: 'BRE', name: 'Brittany', cities: [{ name: 'Rennes' }, { name: 'Brest' }] },
        ]
    },
    {
        code: 'DE', name: 'Germany', states: [
            { code: 'BY', name: 'Bavaria', cities: [{ name: 'Munich' }, { name: 'Nuremberg' }, { name: 'Augsburg' }, { name: 'Regensburg' }, { name: 'Ingolstadt' }] },
            { code: 'BE', name: 'Berlin', cities: [{ name: 'Berlin' }] },
            { code: 'HH', name: 'Hamburg', cities: [{ name: 'Hamburg' }] },
            { code: 'NW', name: 'North Rhine-Westphalia', cities: [{ name: 'Cologne' }, { name: 'Dusseldorf' }, { name: 'Dortmund' }, { name: 'Essen' }, { name: 'Duisburg' }, { name: 'Bochum' }, { name: 'Wuppertal' }, { name: 'Bielefeld' }] },
            { code: 'BW', name: 'Baden-Wurttemberg', cities: [{ name: 'Stuttgart' }, { name: 'Karlsruhe' }, { name: 'Freiburg' }, { name: 'Heidelberg' }, { name: 'Ulm' }] },
            { code: 'HE', name: 'Hesse', cities: [{ name: 'Frankfurt' }, { name: 'Wiesbaden' }, { name: 'Kassel' }] },
            { code: 'NI', name: 'Lower Saxony', cities: [{ name: 'Hanover' }, { name: 'Braunschweig' }] },
            { code: 'SN', name: 'Saxony', cities: [{ name: 'Dresden' }, { name: 'Leipzig' }] },
        ]
    },
    {
        code: 'GR', name: 'Greece', states: [
            { code: 'AT', name: 'Attica', cities: [{ name: 'Athens' }, { name: 'Piraeus' }] },
            { code: 'CM', name: 'Central Macedonia', cities: [{ name: 'Thessaloniki' }] },
        ]
    },
    { code: 'HU', name: 'Hungary', states: [] },
    { code: 'IS', name: 'Iceland', states: [] },
    {
        code: 'IE', name: 'Ireland', states: [
            { code: 'L', name: 'Leinster', cities: [{ name: 'Dublin' }, { name: 'Kilkenny' }] },
            { code: 'M', name: 'Munster', cities: [{ name: 'Cork' }, { name: 'Limerick' }, { name: 'Waterford' }] },
            { code: 'C', name: 'Connacht', cities: [{ name: 'Galway' }] },
        ]
    },
    {
        code: 'IT', name: 'Italy', states: [
            { code: 'LOM', name: 'Lombardy', cities: [{ name: 'Milan' }, { name: 'Brescia' }, { name: 'Bergamo' }] },
            { code: 'LAZ', name: 'Lazio', cities: [{ name: 'Rome' }] },
            { code: 'CAM', name: 'Campania', cities: [{ name: 'Naples' }, { name: 'Salerno' }] },
            { code: 'SIC', name: 'Sicily', cities: [{ name: 'Palermo' }, { name: 'Catania' }] },
            { code: 'VEN', name: 'Veneto', cities: [{ name: 'Venice' }, { name: 'Verona' }, { name: 'Padua' }] },
            { code: 'TUS', name: 'Tuscany', cities: [{ name: 'Florence' }, { name: 'Pisa' }, { name: 'Siena' }] },
            { code: 'PIE', name: 'Piedmont', cities: [{ name: 'Turin' }] },
        ]
    },
    { code: 'XK', name: 'Kosovo', states: [] },
    { code: 'LV', name: 'Latvia', states: [] },
    { code: 'LI', name: 'Liechtenstein', states: [] },
    { code: 'LT', name: 'Lithuania', states: [] },
    { code: 'LU', name: 'Luxembourg', states: [] },
    { code: 'MT', name: 'Malta', states: [] },
    { code: 'MD', name: 'Moldova', states: [] },
    { code: 'MC', name: 'Monaco', states: [] },
    { code: 'ME', name: 'Montenegro', states: [] },
    {
        code: 'NL', name: 'Netherlands', states: [
            { code: 'NH', name: 'North Holland', cities: [{ name: 'Amsterdam' }, { name: 'Haarlem' }, { name: 'Almere' }] },
            { code: 'ZH', name: 'South Holland', cities: [{ name: 'Rotterdam' }, { name: 'The Hague' }, { name: 'Delft' }] },
            { code: 'UT', name: 'Utrecht', cities: [{ name: 'Utrecht' }] },
            { code: 'NB', name: 'North Brabant', cities: [{ name: 'Eindhoven' }, { name: 'Tilburg' }, { name: 'Breda' }] },
        ]
    },
    { code: 'MK', name: 'North Macedonia', states: [] },
    {
        code: 'NO', name: 'Norway', states: [
            { code: 'OSL', name: 'Oslo', cities: [{ name: 'Oslo' }] },
            { code: 'VK', name: 'Viken', cities: [{ name: 'Drammen' }, { name: 'Fredrikstad' }] },
            { code: 'HOR', name: 'Vestland', cities: [{ name: 'Bergen' }] },
        ]
    },
    {
        code: 'PL', name: 'Poland', states: [
            { code: 'MAZ', name: 'Masovian', cities: [{ name: 'Warsaw' }, { name: 'Radom' }] },
            { code: 'MLP', name: 'Lesser Poland', cities: [{ name: 'Krakow' }, { name: 'Tarnow' }] },
            { code: 'SIL', name: 'Silesian', cities: [{ name: 'Katowice' }, { name: 'Czestochowa' }, { name: 'Sosnowiec' }] },
            { code: 'LDS', name: 'Lodz', cities: [{ name: 'Lodz' }] },
        ]
    },
    {
        code: 'PT', name: 'Portugal', states: [
            { code: 'LIS', name: 'Lisbon', cities: [{ name: 'Lisbon' }, { name: 'Amadora' }, { name: 'Setubal' }] },
            { code: 'POR', name: 'Porto', cities: [{ name: 'Porto' }, { name: 'Vila Nova de Gaia' }, { name: 'Braga' }] },
        ]
    },
    { code: 'RO', name: 'Romania', states: [] },
    {
        code: 'RU', name: 'Russia', states: [
            { code: 'MOW', name: 'Moscow', cities: [{ name: 'Moscow' }] },
            { code: 'SPE', name: 'Saint Petersburg', cities: [{ name: 'Saint Petersburg' }] },
            { code: 'SVE', name: 'Sverdlovsk', cities: [{ name: 'Yekaterinburg' }] },
            { code: 'TA', name: 'Tatarstan', cities: [{ name: 'Kazan' }] },
            { code: 'KDA', name: 'Krasnodar Krai', cities: [{ name: 'Krasnodar' }, { name: 'Sochi' }] },
        ]
    },
    { code: 'SM', name: 'San Marino', states: [] },
    { code: 'RS', name: 'Serbia', states: [] },
    { code: 'SK', name: 'Slovakia', states: [] },
    { code: 'SI', name: 'Slovenia', states: [] },
    {
        code: 'ES', name: 'Spain', states: [
            { code: 'MD', name: 'Community of Madrid', cities: [{ name: 'Madrid' }, { name: 'Alcala de Henares' }] },
            { code: 'CT', name: 'Catalonia', cities: [{ name: 'Barcelona' }, { name: 'Hospitalet de Llobregat' }, { name: 'Badalona' }] },
            { code: 'AN', name: 'Andalusia', cities: [{ name: 'Seville' }, { name: 'Malaga' }, { name: 'Cordoba' }, { name: 'Granada' }] },
            { code: 'VC', name: 'Valencian Community', cities: [{ name: 'Valencia' }, { name: 'Alicante' }] },
            { code: 'PV', name: 'Basque Country', cities: [{ name: 'Bilbao' }, { name: 'San Sebastian' }] },
        ]
    },
    {
        code: 'SE', name: 'Sweden', states: [
            { code: 'AB', name: 'Stockholm County', cities: [{ name: 'Stockholm' }, { name: 'Sollentuna' }] },
            { code: 'O', name: 'Vastra Gotaland', cities: [{ name: 'Gothenburg' }, { name: 'Boras' }] },
            { code: 'M', name: 'Skane', cities: [{ name: 'Malmo' }, { name: 'Helsingborg' }] },
        ]
    },
    {
        code: 'CH', name: 'Switzerland', states: [
            { code: 'ZH', name: 'Zurich', cities: [{ name: 'Zurich' }, { name: 'Winterthur' }] },
            { code: 'GE', name: 'Geneva', cities: [{ name: 'Geneva' }] },
            { code: 'BE', name: 'Bern', cities: [{ name: 'Bern' }] },
            { code: 'VD', name: 'Vaud', cities: [{ name: 'Lausanne' }] },
            { code: 'BS', name: 'Basel-City', cities: [{ name: 'Basel' }] },
        ]
    },
    { code: 'UA', name: 'Ukraine', states: [] },
    {
        code: 'GB', name: 'United Kingdom', states: [
            {
                code: 'ENG', name: 'England', cities: [
                    { name: 'London' }, { name: 'Manchester' }, { name: 'Birmingham' }, { name: 'Liverpool' },
                    { name: 'Leeds' }, { name: 'Sheffield' }, { name: 'Bristol' }, { name: 'Newcastle' },
                    { name: 'Oxford' }, { name: 'Cambridge' }, { name: 'Leicester' }, { name: 'Nottingham' },
                    { name: 'Coventry' }, { name: 'Bradford' },
                ]
            },
            { code: 'SCT', name: 'Scotland', cities: [{ name: 'Edinburgh' }, { name: 'Glasgow' }, { name: 'Aberdeen' }, { name: 'Dundee' }, { name: 'Inverness' }] },
            { code: 'WLS', name: 'Wales', cities: [{ name: 'Cardiff' }, { name: 'Swansea' }, { name: 'Newport' }, { name: 'Bangor' }] },
            { code: 'NIR', name: 'Northern Ireland', cities: [{ name: 'Belfast' }, { name: 'Londonderry' }, { name: 'Lisburn' }, { name: 'Newry' }, { name: 'Armagh' }] },
        ]
    },
    { code: 'VA', name: 'Vatican City', states: [] },

    // Oceania
    {
        code: 'AU', name: 'Australia', states: [
            { code: 'NSW', name: 'New South Wales', cities: [{ name: 'Sydney' }, { name: 'Newcastle' }, { name: 'Wollongong' }, { name: 'Central Coast' }, { name: 'Maitland' }] },
            { code: 'VIC', name: 'Victoria', cities: [{ name: 'Melbourne' }, { name: 'Geelong' }, { name: 'Ballarat' }, { name: 'Bendigo' }, { name: 'Shepparton' }] },
            { code: 'QLD', name: 'Queensland', cities: [{ name: 'Brisbane' }, { name: 'Gold Coast' }, { name: 'Sunshine Coast' }, { name: 'Townsville' }, { name: 'Cairns' }] },
            { code: 'WA', name: 'Western Australia', cities: [{ name: 'Perth' }, { name: 'Bunbury' }, { name: 'Rockingham' }, { name: 'Mandurah' }, { name: 'Kalgoorlie' }] },
            { code: 'SA', name: 'South Australia', cities: [{ name: 'Adelaide' }, { name: 'Mount Gambier' }, { name: 'Whyalla' }, { name: 'Port Augusta' }] },
            { code: 'TAS', name: 'Tasmania', cities: [{ name: 'Hobart' }, { name: 'Launceston' }] },
            { code: 'ACT', name: 'Australian Capital Territory', cities: [{ name: 'Canberra' }] },
            { code: 'NT', name: 'Northern Territory', cities: [{ name: 'Darwin' }, { name: 'Alice Springs' }] },
        ]
    },
    { code: 'FJ', name: 'Fiji', states: [] },
    { code: 'KI', name: 'Kiribati', states: [] },
    { code: 'MH', name: 'Marshall Islands', states: [] },
    { code: 'FM', name: 'Micronesia', states: [] },
    { code: 'NR', name: 'Nauru', states: [] },
    {
        code: 'NZ', name: 'New Zealand', states: [
            { code: 'AUK', name: 'Auckland', cities: [{ name: 'Auckland' }, { name: 'Manukau City' }] },
            { code: 'WGN', name: 'Wellington', cities: [{ name: 'Wellington' }, { name: 'Lower Hutt' }] },
            { code: 'CAN', name: 'Canterbury', cities: [{ name: 'Christchurch' }] },
        ]
    },
    { code: 'PW', name: 'Palau', states: [] },
    { code: 'PG', name: 'Papua New Guinea', states: [] },
    { code: 'WS', name: 'Samoa', states: [] },
    { code: 'SB', name: 'Solomon Islands', states: [] },
    { code: 'TO', name: 'Tonga', states: [] },
    { code: 'TV', name: 'Tuvalu', states: [] },
    { code: 'VU', name: 'Vanuatu', states: [] },
];

/** Countries sorted alphabetically by name */
export const COUNTRIES_SORTED = [...COUNTRIES].sort((a, b) => a.name.localeCompare(b.name));

/** Look up country by ISO alpha-2 code (case-insensitive) */
export function findCountry(code: string): CountryData | undefined {
    return COUNTRIES.find(c => c.code.toUpperCase() === code.toUpperCase());
}

/** Look up state by country code + state code */
export function findState(countryCode: string, stateCode: string): StateData | undefined {
    return findCountry(countryCode)?.states.find(s => s.code === stateCode);
}
